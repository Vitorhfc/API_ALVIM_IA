using Admin_Repository.Repositorio.Interface;
using Client_Repository.Configuration.Contexto.Interface;
using Client_Service.Service.Interface;
using Microsoft.Extensions.Logging;
using Shared.Services.Interface;
using Shared.Utils.Criptografia;
using System.Text.RegularExpressions;

namespace Client_Service.Service
{
    public class WebhookAuthService : IWebhookAuthService
    {
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;
        private readonly IContextoMultiTenantService _contextoMultiTenant;
        private readonly IDatabaseProvisioningService _databaseProvisioningService;
        private readonly ILogger<WebhookAuthService> _logger;

        public WebhookAuthService(
            IEmpresaRepository empresaRepository,
            IUsuarioEmpresaRepository usuarioEmpresaRepository,
            IContextoMultiTenantService contextoMultiTenant,
            IDatabaseProvisioningService databaseProvisioningService,
            ILogger<WebhookAuthService> logger)
        {
            _empresaRepository = empresaRepository;
            _usuarioEmpresaRepository = usuarioEmpresaRepository;
            _contextoMultiTenant = contextoMultiTenant;
            _databaseProvisioningService = databaseProvisioningService;
            _logger = logger;
        }

        public async Task<WebhookAuthResult?> AutenticarPorSessionAsync(string sessionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionName))
                {
                    _logger.LogWarning("Session name vazio ou nulo");
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = "Session name é obrigatório"
                    };
                }

                _logger.LogInformation("Buscando empresa por session WAHA: {Session}", sessionName);

                // Buscar empresa pelo session name do WAHA
                var empresa = await _empresaRepository.BuscarPrimeiroPorFiltroAsync(e =>
                    e.WahaSessionName == sessionName &&
                    e.FlgAtivo == true);

                if (empresa == null)
                {
                    _logger.LogWarning("Empresa não encontrada para a session: {Session}", sessionName);
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = $"Empresa não encontrada para a session {sessionName}"
                    };
                }

                _logger.LogInformation(
                    "Empresa encontrada: {EmpresaId} - {NomeFantasia}",
                    empresa.Id,
                    empresa.Nome ?? empresa.RazaoSocial);

                // Buscar o primeiro usuário vinculado à empresa
                var usuariosVinculados = await _usuarioEmpresaRepository.BuscarPorEmpresaIdAsync(empresa.Id);
                var primeiroUsuario = usuariosVinculados.FirstOrDefault();

                if (primeiroUsuario == null)
                {
                    _logger.LogWarning(
                        "Nenhum usuário vinculado à empresa {EmpresaId}. Não é possível processar webhook.",
                        empresa.Id);
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = $"Nenhum usuário vinculado à empresa {empresa.Nome ?? empresa.RazaoSocial}"
                    };
                }

                var usuarioId = primeiroUsuario.UsuarioId;

                _logger.LogInformation(
                    "Usuário selecionado para webhook: {UsuarioId} da empresa: {EmpresaId}",
                    usuarioId,
                    empresa.Id);

                // Configurar contexto do tenant com usuário real vinculado
                try
                {
                    // ✅ PROVISIONAMENTO AUTOMÁTICO se empresa não tem banco configurado
                    if (string.IsNullOrWhiteSpace(empresa.ConnectionString))
                    {
                        _logger.LogWarning(
                            "Empresa {EmpresaId} sem banco configurado. Iniciando provisionamento automático...",
                            empresa.Id);

                        var resultadoProvisionamento = await _databaseProvisioningService
                            .ProvisionarBancoDadosEmpresaAsync(
                                empresa.Id,
                                empresa.Nome ?? empresa.RazaoSocial ?? "Empresa");

                        if (!resultadoProvisionamento.Sucesso)
                        {
                            _logger.LogError(
                                "Falha ao provisionar banco automaticamente para empresa {EmpresaId}: {Erro}",
                                empresa.Id,
                                resultadoProvisionamento.Erro);
                            return new WebhookAuthResult
                            {
                                Sucesso = false,
                                MensagemErro = $"Falha ao provisionar banco: {resultadoProvisionamento.Erro}"
                            };
                        }

                        _logger.LogInformation(
                            "Banco provisionado automaticamente para empresa {EmpresaId}: {Database}",
                            empresa.Id,
                            resultadoProvisionamento.NomeBaseDados);

                        // Atualizar objeto empresa com os dados provisionados
                        empresa.ConnectionString = Criptografia.Encriptar(resultadoProvisionamento.ConnectionString!);
                        empresa.NomeBaseDados = resultadoProvisionamento.NomeBaseDados;
                    }

                    if (string.IsNullOrWhiteSpace(empresa.NomeBaseDados))
                    {
                        _logger.LogError("Nome da base de dados da empresa {EmpresaId} está vazio", empresa.Id);
                        return new WebhookAuthResult
                        {
                            Sucesso = false,
                            MensagemErro = "Nome da base de dados não configurado"
                        };
                    }

                    // Descriptografar connection string
                    string connectionString;
                    try
                    {
                        connectionString = Criptografia.Desencripitar(empresa.ConnectionString);

                        if (string.IsNullOrWhiteSpace(connectionString))
                        {
                            throw new Exception("Connection string descriptografada está vazia");
                        }

                        _logger.LogInformation(
                            "Connection string descriptografada com sucesso para empresa {EmpresaId}",
                            empresa.Id);
                    }
                    catch (Exception decryptEx)
                    {
                        _logger.LogError(
                            decryptEx,
                            "Erro ao descriptografar connection string da empresa {EmpresaId}",
                            empresa.Id);
                        return new WebhookAuthResult
                        {
                            Sucesso = false,
                            MensagemErro = $"Erro ao descriptografar connection string: {decryptEx.Message}"
                        };
                    }

                    var nomeBaseDados = empresa.NomeBaseDados;

                    // Configurar tenant com usuário real vinculado à empresa
                    _contextoMultiTenant.ConfigurarTenant(
                        usuarioId,
                        empresa.Id,
                        connectionString,
                        nomeBaseDados
                    );

                    _logger.LogInformation(
                        "Contexto webhook configurado - UsuarioId: {UsuarioId}, EmpresaId: {EmpresaId}, DB: {Database}, Session: {Session}",
                        usuarioId,
                        empresa.Id,
                        nomeBaseDados,
                        sessionName);

                    return new WebhookAuthResult
                    {
                        EmpresaId = empresa.Id,
                        UsuarioId = usuarioId,
                        ConnectionString = connectionString,
                        NomeBaseDados = nomeBaseDados,
                        SessionName = sessionName,
                        NumeroWhatsApp = empresa.WahaNumeroWhatsApp,
                        Sucesso = true
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao configurar contexto do tenant para webhook");
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = $"Erro ao configurar contexto: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao autenticar por session WAHA");
                return new WebhookAuthResult
                {
                    Sucesso = false,
                    MensagemErro = $"Erro na autenticação: {ex.Message}"
                };
            }
        }

        public async Task<WebhookAuthResult?> AutenticarPorNumeroWhatsAppAsync(string numeroWhatsApp)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(numeroWhatsApp))
                {
                    _logger.LogWarning("Número do WhatsApp vazio ou nulo");
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = "Número do WhatsApp é obrigatório"
                    };
                }

                // Extrair número limpo
                var numeroLimpo = ExtrairNumeroLimpo(numeroWhatsApp);

                _logger.LogInformation("Buscando empresa por número WhatsApp: {Numero}", numeroLimpo);

                // Buscar empresa pelo número do WhatsApp
                var empresa = await _empresaRepository.BuscarPrimeiroPorFiltroAsync(e =>
                    e.WahaNumeroWhatsApp == numeroLimpo &&
                    e.FlgAtivo == true);

                if (empresa == null)
                {
                    _logger.LogWarning("Empresa não encontrada para o número: {Numero}", numeroLimpo);
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = $"Empresa não encontrada para o número {numeroLimpo}"
                    };
                }

                _logger.LogInformation(
                    "Empresa encontrada: {EmpresaId} - {NomeFantasia}",
                    empresa.Id,
                    empresa.Nome ?? empresa.RazaoSocial);

                // Buscar o primeiro usuário vinculado à empresa
                var usuariosVinculados = await _usuarioEmpresaRepository.BuscarPorEmpresaIdAsync(empresa.Id);
                var primeiroUsuario = usuariosVinculados.FirstOrDefault();

                if (primeiroUsuario == null)
                {
                    _logger.LogWarning(
                        "Nenhum usuário vinculado à empresa {EmpresaId}. Não é possível processar webhook.",
                        empresa.Id);
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = $"Nenhum usuário vinculado à empresa {empresa.Nome ?? empresa.RazaoSocial}"
                    };
                }

                var usuarioId = primeiroUsuario.UsuarioId;

                _logger.LogInformation(
                    "Usuário selecionado para webhook: {UsuarioId} da empresa: {EmpresaId}",
                    usuarioId,
                    empresa.Id);

                // Configurar contexto do tenant com usuário real vinculado
                try
                {
                    // ✅ PROVISIONAMENTO AUTOMÁTICO se empresa não tem banco configurado
                    if (string.IsNullOrWhiteSpace(empresa.ConnectionString))
                    {
                        _logger.LogWarning(
                            "Empresa {EmpresaId} sem banco configurado. Iniciando provisionamento automático...",
                            empresa.Id);

                        var resultadoProvisionamento = await _databaseProvisioningService
                            .ProvisionarBancoDadosEmpresaAsync(
                                empresa.Id,
                                empresa.Nome ?? empresa.RazaoSocial ?? "Empresa");

                        if (!resultadoProvisionamento.Sucesso)
                        {
                            _logger.LogError(
                                "Falha ao provisionar banco automaticamente para empresa {EmpresaId}: {Erro}",
                                empresa.Id,
                                resultadoProvisionamento.Erro);
                            return new WebhookAuthResult
                            {
                                Sucesso = false,
                                MensagemErro = $"Falha ao provisionar banco: {resultadoProvisionamento.Erro}"
                            };
                        }

                        _logger.LogInformation(
                            "Banco provisionado automaticamente para empresa {EmpresaId}: {Database}",
                            empresa.Id,
                            resultadoProvisionamento.NomeBaseDados);

                        // Atualizar objeto empresa com os dados provisionados
                        empresa.ConnectionString = Criptografia.Encriptar(resultadoProvisionamento.ConnectionString!);
                        empresa.NomeBaseDados = resultadoProvisionamento.NomeBaseDados;
                    }

                    if (string.IsNullOrWhiteSpace(empresa.NomeBaseDados))
                    {
                        _logger.LogError("Nome da base de dados da empresa {EmpresaId} está vazio", empresa.Id);
                        return new WebhookAuthResult
                        {
                            Sucesso = false,
                            MensagemErro = "Nome da base de dados não configurado"
                        };
                    }

                    // Descriptografar connection string
                    string connectionString;
                    try
                    {
                        connectionString = Criptografia.Desencripitar(empresa.ConnectionString);

                        if (string.IsNullOrWhiteSpace(connectionString))
                        {
                            throw new Exception("Connection string descriptografada está vazia");
                        }

                        _logger.LogInformation(
                            "Connection string descriptografada com sucesso para empresa {EmpresaId}",
                            empresa.Id);
                    }
                    catch (Exception decryptEx)
                    {
                        _logger.LogError(
                            decryptEx,
                            "Erro ao descriptografar connection string da empresa {EmpresaId}",
                            empresa.Id);
                        return new WebhookAuthResult
                        {
                            Sucesso = false,
                            MensagemErro = $"Erro ao descriptografar connection string: {decryptEx.Message}"
                        };
                    }

                    var nomeBaseDados = empresa.NomeBaseDados;

                    // Configurar tenant com usuário real vinculado à empresa
                    _contextoMultiTenant.ConfigurarTenant(
                        usuarioId,
                        empresa.Id,
                        connectionString,
                        nomeBaseDados
                    );

                    _logger.LogInformation(
                        "Contexto webhook configurado - UsuarioId: {UsuarioId}, EmpresaId: {EmpresaId}, DB: {Database}",
                        usuarioId,
                        empresa.Id,
                        nomeBaseDados);

                    return new WebhookAuthResult
                    {
                        EmpresaId = empresa.Id,
                        UsuarioId = usuarioId,
                        ConnectionString = connectionString,
                        NomeBaseDados = nomeBaseDados,
                        NumeroWhatsApp = numeroLimpo,
                        Sucesso = true
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao configurar contexto do tenant para webhook");
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = $"Erro ao configurar contexto: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao autenticar por número do WhatsApp");
                return new WebhookAuthResult
                {
                    Sucesso = false,
                    MensagemErro = $"Erro na autenticação: {ex.Message}"
                };
            }
        }

        public async Task<WebhookAuthResult?> AutenticarPorEmpresaIdAsync(string empresaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empresaId))
                {
                    _logger.LogWarning("ID da empresa vazio ou nulo");
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = "ID da empresa é obrigatório"
                    };
                }

                _logger.LogInformation("Buscando empresa por ID: {EmpresaId}", empresaId);

                // Buscar empresa pelo ID
                var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);

                if (empresa == null || empresa.FlgAtivo != true)
                {
                    _logger.LogWarning("Empresa não encontrada ou inativa para o ID: {EmpresaId}", empresaId);
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = $"Empresa não encontrada ou inativa para o ID {empresaId}"
                    };
                }

                _logger.LogInformation(
                    "Empresa encontrada: {EmpresaId} - {NomeFantasia}",
                    empresa.Id,
                    empresa.Nome ?? empresa.RazaoSocial);

                // Buscar o primeiro usuário vinculado à empresa
                var usuariosVinculados = await _usuarioEmpresaRepository.BuscarPorEmpresaIdAsync(empresa.Id);
                var primeiroUsuario = usuariosVinculados.FirstOrDefault();

                if (primeiroUsuario == null)
                {
                    _logger.LogWarning(
                        "Nenhum usuário vinculado à empresa {EmpresaId}. Não é possível processar webhook.",
                        empresa.Id);
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = $"Nenhum usuário vinculado à empresa {empresa.Nome ?? empresa.RazaoSocial}"
                    };
                }

                var usuarioId = primeiroUsuario.UsuarioId;

                _logger.LogInformation(
                    "Usuário selecionado para webhook: {UsuarioId} da empresa: {EmpresaId}",
                    usuarioId,
                    empresa.Id);

                // Configurar contexto do tenant com usuário real vinculado
                try
                {
                    // ✅ PROVISIONAMENTO AUTOMÁTICO se empresa não tem banco configurado
                    if (string.IsNullOrWhiteSpace(empresa.ConnectionString))
                    {
                        _logger.LogWarning(
                            "Empresa {EmpresaId} sem banco configurado. Iniciando provisionamento automático...",
                            empresa.Id);

                        var resultadoProvisionamento = await _databaseProvisioningService
                            .ProvisionarBancoDadosEmpresaAsync(
                                empresa.Id,
                                empresa.Nome ?? empresa.RazaoSocial ?? "Empresa");

                        if (!resultadoProvisionamento.Sucesso)
                        {
                            _logger.LogError(
                                "Falha ao provisionar banco automaticamente para empresa {EmpresaId}: {Erro}",
                                empresa.Id,
                                resultadoProvisionamento.Erro);
                            return new WebhookAuthResult
                            {
                                Sucesso = false,
                                MensagemErro = $"Falha ao provisionar banco: {resultadoProvisionamento.Erro}"
                            };
                        }

                        _logger.LogInformation(
                            "Banco provisionado automaticamente para empresa {EmpresaId}: {Database}",
                            empresa.Id,
                            resultadoProvisionamento.NomeBaseDados);

                        // Atualizar objeto empresa com os dados provisionados
                        empresa.ConnectionString = Criptografia.Encriptar(resultadoProvisionamento.ConnectionString!);
                        empresa.NomeBaseDados = resultadoProvisionamento.NomeBaseDados;
                    }

                    if (string.IsNullOrWhiteSpace(empresa.NomeBaseDados))
                    {
                        _logger.LogError("Nome da base de dados da empresa {EmpresaId} está vazio", empresa.Id);
                        return new WebhookAuthResult
                        {
                            Sucesso = false,
                            MensagemErro = "Nome da base de dados não configurado"
                        };
                    }

                    // Descriptografar connection string
                    string connectionString;
                    try
                    {
                        connectionString = Criptografia.Desencripitar(empresa.ConnectionString);

                        if (string.IsNullOrWhiteSpace(connectionString))
                        {
                            throw new Exception("Connection string descriptografada está vazia");
                        }

                        _logger.LogInformation(
                            "Connection string descriptografada com sucesso para empresa {EmpresaId}",
                            empresa.Id);
                    }
                    catch (Exception decryptEx)
                    {
                        _logger.LogError(
                            decryptEx,
                            "Erro ao descriptografar connection string da empresa {EmpresaId}",
                            empresa.Id);
                        return new WebhookAuthResult
                        {
                            Sucesso = false,
                            MensagemErro = $"Erro ao descriptografar connection string: {decryptEx.Message}"
                        };
                    }

                    var nomeBaseDados = empresa.NomeBaseDados;

                    // Configurar tenant com usuário real vinculado à empresa
                    _contextoMultiTenant.ConfigurarTenant(
                        usuarioId,
                        empresa.Id,
                        connectionString,
                        nomeBaseDados
                    );

                    _logger.LogInformation(
                        "Contexto webhook configurado - UsuarioId: {UsuarioId}, EmpresaId: {EmpresaId}, DB: {Database}",
                        usuarioId,
                        empresa.Id,
                        nomeBaseDados);

                    return new WebhookAuthResult
                    {
                        EmpresaId = empresa.Id,
                        UsuarioId = usuarioId,
                        ConnectionString = connectionString,
                        NomeBaseDados = nomeBaseDados,
                        SessionName = empresa.WahaSessionName,
                        NumeroWhatsApp = empresa.WahaNumeroWhatsApp,
                        Sucesso = true
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao configurar contexto do tenant para webhook");
                    return new WebhookAuthResult
                    {
                        Sucesso = false,
                        MensagemErro = $"Erro ao configurar contexto: {ex.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao autenticar por ID da empresa");
                return new WebhookAuthResult
                {
                    Sucesso = false,
                    MensagemErro = $"Erro na autenticação: {ex.Message}"
                };
            }
        }

        public string ExtrairNumeroLimpo(string numeroWaha)
        {
            if (string.IsNullOrWhiteSpace(numeroWaha))
                return string.Empty;

            // Remover sufixos comuns do WAHA
            var numeroLimpo = numeroWaha
                .Replace("@c.us", "")
                .Replace("@s.whatsapp.net", "")
                .Replace("@g.us", "")
                .Replace("@broadcast", "")
                .Replace(":", "")
                .Trim();

            // Extrair apenas números
            numeroLimpo = Regex.Replace(numeroLimpo, @"[^\d]", "");

            return numeroLimpo;
        }
    }
}
