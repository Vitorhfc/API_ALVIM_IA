using Admin_Repository.Repositorio.Interface;
using Admin_Service.Service.Interface;
using Admin_Service.ServiceGenerico;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.ADM;
using Shared.Services.Interface;
using Shared.Utils;
using Shared.Utils.Criptografia;

namespace Admin_Service.Service
{
    public class EmpresaService : ServiceGenerico<Empresa>, IEmpresaService
    {
        #region Campos

        private readonly IEmpresaRepository _empresaRepository;
        private readonly IUsuarioEmpresaService _usuarioEmpresaService;
        private readonly IDatabaseProvisioningService _databaseProvisioningService;
        private readonly ILogger<EmpresaService> _logger;

        #endregion

        #region Construtor

        public EmpresaService(
            IEmpresaRepository empresaRepository,
            IUsuarioEmpresaService usuarioEmpresaService,
            IDatabaseProvisioningService databaseProvisioningService,
            ILogger<EmpresaService> logger)
            : base(empresaRepository)
        {
            _empresaRepository = empresaRepository;
            _usuarioEmpresaService = usuarioEmpresaService;
            _databaseProvisioningService = databaseProvisioningService;
            _logger = logger;
        }

        #endregion

        #region Métodos Específicos

        public async Task<Empresa?> BuscarPorCnpjAsync(string cnpj)
        {
            try
            {
                return await _empresaRepository.BuscarPorCnpjAsync(cnpj);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar empresa por CNPJ: {ex.Message}", ex);
            }
        }

        public async Task<Empresa?> BuscarPorEmailAsync(string email)
        {
            try
            {
                return await _empresaRepository.BuscarPorEmailAsync(email);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar empresa por email: {ex.Message}", ex);
            }
        }

        public async Task<Empresa?> BuscarPorSessionNameAsync(string sessionName)
        {
            try
            {
                return await _empresaRepository.BuscarPorSessionNameAsync(sessionName);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao buscar empresa por session name: {ex.Message}", ex);
            }
        }

        public async Task<Empresa?> CadastrarEmpresaComUsuarioAsync(Empresa empresa, string usuarioId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuarioId))
                    throw new ArgumentException("UsuarioId é obrigatório para cadastrar uma empresa", nameof(usuarioId));

                await ValidarEmpresaParaCriacaoAsync(empresa);

                // Se a connection string foi fornecida manualmente, criptografar
                if (!string.IsNullOrWhiteSpace(empresa.ConnectionString))
                {
                    empresa.ConnectionString = Criptografia.Encriptar(empresa.ConnectionString);
                }

                empresa.DtaCadastro = DateTime.Now;
                var empresaCriada = await _empresaRepository.AdicionarAsync(empresa);

                if (empresaCriada == null)
                    throw new ApplicationException("Erro ao criar empresa");

                // Criar vínculo usuário-empresa
                var usuarioEmpresa = new UsuarioEmpresa
                {
                    UsuarioId = usuarioId,
                    EmpresaId = empresaCriada.Id,
                    FlgAdministrador = true,
                    DtaCadastro = DateTime.Now
                };

                await _usuarioEmpresaService.AdicionarAsync(usuarioEmpresa);

                // ✅ PROVISIONAMENTO AUTOMÁTICO DE BANCO DE DADOS
                // Se a empresa não tem connection string configurada, provisionar automaticamente
                if (string.IsNullOrWhiteSpace(empresa.ConnectionString))
                {
                    _logger.LogInformation(
                        "Iniciando provisionamento automático de banco para empresa {EmpresaId}",
                        empresaCriada.Id);

                    var resultadoProvisionamento = await _databaseProvisioningService
                        .ProvisionarBancoDadosEmpresaAsync(
                            empresaCriada.Id,
                            empresaCriada.Nome ?? empresaCriada.RazaoSocial ?? "Empresa");

                    if (resultadoProvisionamento.Sucesso)
                    {
                        _logger.LogInformation(
                            "Banco provisionado automaticamente para empresa {EmpresaId}: {Database}",
                            empresaCriada.Id,
                            resultadoProvisionamento.NomeBaseDados);

                        // Atualizar objeto retornado com os dados do banco
                        empresaCriada.ConnectionString = Criptografia.Encriptar(resultadoProvisionamento.ConnectionString!);
                        empresaCriada.NomeBaseDados = resultadoProvisionamento.NomeBaseDados;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Falha ao provisionar banco automaticamente para empresa {EmpresaId}: {Erro}",
                            empresaCriada.Id,
                            resultadoProvisionamento.Erro);
                    }
                }

                return empresaCriada;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar empresa com usuário");
                throw new ApplicationException($"Erro no serviço ao criar empresa com usuário: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Provisiona banco de dados para uma empresa existente que não tem banco configurado
        /// </summary>
        public async Task<Empresa?> ProvisionarBancoDadosAsync(string empresaId)
        {
            try
            {
                var empresa = await _empresaRepository.BuscarPorIdAsync(empresaId);
                if (empresa == null)
                {
                    throw new ApplicationException($"Empresa com ID {empresaId} não encontrada");
                }

                // Verificar se já tem banco provisionado
                if (!string.IsNullOrWhiteSpace(empresa.ConnectionString))
                {
                    _logger.LogInformation(
                        "Empresa {EmpresaId} já possui banco provisionado",
                        empresaId);
                    return empresa;
                }

                _logger.LogInformation(
                    "Provisionando banco de dados para empresa {EmpresaId}",
                    empresaId);

                var resultado = await _databaseProvisioningService
                    .ProvisionarBancoDadosEmpresaAsync(
                        empresaId,
                        empresa.Nome ?? empresa.RazaoSocial ?? "Empresa");

                if (!resultado.Sucesso)
                {
                    throw new ApplicationException(
                        $"Falha ao provisionar banco: {resultado.Erro}");
                }

                _logger.LogInformation(
                    "Banco provisionado com sucesso para empresa {EmpresaId}: {Database}",
                    empresaId,
                    resultado.NomeBaseDados);

                // Buscar empresa atualizada
                return await _empresaRepository.BuscarPorIdAsync(empresaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao provisionar banco para empresa {EmpresaId}", empresaId);
                throw new ApplicationException(
                    $"Erro ao provisionar banco de dados: {ex.Message}", ex);
            }
        }

        public override async Task<Empresa?> EditarAsync(Empresa empresa)
        {
            try
            {
                await ValidarEmpresaParaEdicaoAsync(empresa);

                // Não recriptografar se já está criptografada
                // A ConnectionString já vem criptografada do banco quando buscamos a empresa
                // Só criptografar se for uma string não criptografada (ex: quando vem do formulário)
                if (!string.IsNullOrWhiteSpace(empresa.ConnectionString))
                {
                    // Verificar se não está criptografada (strings criptografadas não começam com mongodb:// e não contêm caracteres típicos de connection string)
                    bool ehConnectionStringPlainText = empresa.ConnectionString.StartsWith("mongodb://") ||
                                                       empresa.ConnectionString.StartsWith("mongodb+srv://") ||
                                                       empresa.ConnectionString.Contains("://") ||
                                                       empresa.ConnectionString.Contains("Server=") ||
                                                       empresa.ConnectionString.Contains("Data Source=");

                    if (ehConnectionStringPlainText)
                    {
                        empresa.ConnectionString = Criptografia.Encriptar(empresa.ConnectionString);
                    }
                }

                empresa.DtaAlteracao = DateTime.Now;
                return await base.EditarAsync(empresa);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro no serviço ao editar empresa: {ex.Message}", ex);
            }
        }

        #endregion

        #region Validações Específicas

        public override async Task ValidarEntidadeAsync(Empresa entity)
        {
            ValidarCampoObrigatorio(entity.Nome, "Nome da Empresa");
            ValidarCampoObrigatorio(entity.Email, "Email");

            if (!ValidacaoDeDados.ValidarEmail(entity.Email))
                throw new ArgumentException("Email inválido");

            await Task.CompletedTask;
        }

        private async Task ValidarEmpresaParaCriacaoAsync(Empresa empresa)
        {
            await ValidarEntidadeAsync(empresa);

            if (await _empresaRepository.ExisteCnpjAsync(empresa.CNPJ))
                throw new InvalidOperationException("Já existe uma empresa cadastrada com este CNPJ");

            if (await _empresaRepository.ExisteEmailAsync(empresa.Email))
                throw new InvalidOperationException("Já existe uma empresa cadastrada com este email");
        }

        private async Task ValidarEmpresaParaEdicaoAsync(Empresa empresa)
        {
            await ValidarEntidadeAsync(empresa);

            if (await _empresaRepository.ExisteCnpjAsync(empresa.CNPJ, empresa.Id))
                throw new InvalidOperationException("Já existe uma empresa cadastrada com este CNPJ");

            if (await _empresaRepository.ExisteEmailAsync(empresa.Email, empresa.Id))
                throw new InvalidOperationException("Já existe uma empresa cadastrada com este email");
        }

        #endregion
    }
}