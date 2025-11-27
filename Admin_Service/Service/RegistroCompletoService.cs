using Admin_Repository.Repositorio.Interface;
using Admin_Service.Service.Interface;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Model;
using Shared.Classes.ModelView.ADM;

namespace Admin_Service.Service
{
    /// <summary>
    /// Serviço responsável por orquestrar o registro completo de usuário + empresa + autenticação
    /// </summary>
    public class RegistroCompletoService : IRegistroCompletoService
    {
        #region Campos

        private readonly IUsuarioService _usuarioService;
        private readonly IEmpresaService _empresaService;
        private readonly IAutenticacaoService _autenticacaoService;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<RegistroCompletoService> _logger;

        #endregion

        #region Construtor

        public RegistroCompletoService(
            IUsuarioService usuarioService,
            IEmpresaService empresaService,
            IAutenticacaoService autenticacaoService,
            IUsuarioRepository usuarioRepository,
            IEmpresaRepository empresaRepository,
            IUsuarioEmpresaRepository usuarioEmpresaRepository,
            IMapper mapper,
            ILogger<RegistroCompletoService> logger)
        {
            _usuarioService = usuarioService;
            _empresaService = empresaService;
            _autenticacaoService = autenticacaoService;
            _usuarioRepository = usuarioRepository;
            _empresaRepository = empresaRepository;
            _usuarioEmpresaRepository = usuarioEmpresaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Realiza o registro completo em uma transação:
        /// 1. Cadastra o usuário
        /// 2. Cadastra a empresa
        /// 3. Vincula usuário como administrador da empresa
        /// 4. Autentica o usuário (login direto)
        /// 5. Retorna o token de autenticação
        /// </summary>
        public async Task<RegistroCompletoResponseModel> RegistrarUsuarioEEmpresaAsync(RegistroCompletoModel model)
        {
            _logger.LogInformation("Iniciando registro completo para usuário {Email}", model.Email);

            Usuario? usuarioCriado = null;
            Empresa? empresaCriada = null;
            UsuarioEmpresa? vinculoCriado = null;

            try
            {
                // ==================== PASSO 1: Cadastrar Usuário ====================
                _logger.LogInformation("PASSO 1: Cadastrando usuário {Nome}", model.Nome);

                var usuarioModel = new UsuarioModel
                {
                    Nome = model.Nome,
                    Email = model.Email,
                    Cpf = model.Cpf,
                    Celular = model.Celular,
                    Senha = model.Senha,
                    DtaNascimento = model.DtaNascimento
                };

                var usuarioModelCriado = await _usuarioService.CadastrarUsuarioAsync(usuarioModel);
                usuarioCriado = await _usuarioRepository.BuscarPorIdAsync(usuarioModelCriado.Id);

                if (usuarioCriado == null)
                {
                    throw new ApplicationException("Erro ao recuperar usuário criado");
                }

                _logger.LogInformation(
                    "✓ PASSO 1 CONCLUÍDO: Usuário {Nome} cadastrado com ID {UsuarioId}",
                    usuarioCriado.Nome,
                    usuarioCriado.Id
                );

                // ==================== PASSO 2: Cadastrar Empresa ====================
                _logger.LogInformation(
                    "PASSO 2: Cadastrando empresa {RazaoSocial} vinculada ao usuário {UsuarioId}",
                    model.RazaoSocial,
                    usuarioCriado.Id
                );

                var empresa = new Empresa
                {
                    RazaoSocial = model.RazaoSocial,
                    Nome = model.NomeFantasia ?? model.RazaoSocial,
                    CNPJ = model.CNPJ,
                    Email = model.EmailEmpresa,
                    WahaSessionName = model.WahaSessionName,
                    WahaNumeroWhatsApp = model.WahaNumeroWhatsApp,
                    FlgSuspensa = false,
                    FlgWahaAtivo = !string.IsNullOrWhiteSpace(model.WahaSessionName)
                };

                // Este método já cria o vínculo usuário-empresa automaticamente
                empresaCriada = await _empresaService.CadastrarEmpresaComUsuarioAsync(empresa, usuarioCriado.Id);

                if (empresaCriada == null)
                {
                    throw new ApplicationException("Erro ao criar empresa");
                }

                _logger.LogInformation(
                    "✓ PASSO 2 CONCLUÍDO: Empresa {RazaoSocial} cadastrada com ID {EmpresaId}",
                    empresaCriada.RazaoSocial,
                    empresaCriada.Id
                );

                // ==================== PASSO 3: Verificar Vínculo ====================
                _logger.LogInformation(
                    "PASSO 3: Verificando vínculo entre usuário {UsuarioId} e empresa {EmpresaId}",
                    usuarioCriado.Id,
                    empresaCriada.Id
                );

                var vinculos = await _usuarioEmpresaRepository.BuscarPorUsuarioIdAsync(usuarioCriado.Id);
                vinculoCriado = vinculos.FirstOrDefault(v => v.EmpresaId == empresaCriada.Id);

                if (vinculoCriado == null)
                {
                    throw new ApplicationException("Erro ao verificar vínculo usuário-empresa");
                }

                _logger.LogInformation(
                    "✓ PASSO 3 CONCLUÍDO: Vínculo verificado. Usuário é ADMIN: {FlgAdmin}",
                    vinculoCriado.FlgAdministrador
                );

                // ==================== PASSO 4: Autenticar Usuário (Login Direto) ====================
                _logger.LogInformation(
                    "PASSO 4: Autenticando usuário {UsuarioId} automaticamente",
                    usuarioCriado.Id
                );

                var autenticacaoResponse = await _autenticacaoService.LoginUsuarioAsync(new LoginUsuarioModel
                {
                    UsuarioId = usuarioCriado.Id
                });

                _logger.LogInformation(
                    "✓ PASSO 4 CONCLUÍDO: Usuário autenticado com sucesso. Token gerado"
                );

                // ==================== PASSO 5: Montar Response ====================
                _logger.LogInformation("PASSO 5: Montando response final");

                var response = new RegistroCompletoResponseModel
                {
                    UsuarioId = usuarioCriado.Id,
                    Nome = usuarioCriado.Nome,
                    Email = usuarioCriado.Email,
                    EmpresaId = empresaCriada.Id,
                    EmpresaNome = empresaCriada.Nome ?? empresaCriada.RazaoSocial,
                    CNPJ = empresaCriada.CNPJ,
                    Token = autenticacaoResponse.Token,
                    DataExpiracao = autenticacaoResponse.DataExpiracao,
                    Empresas = autenticacaoResponse.Empresas,
                    Mensagem = "Registro completo realizado com sucesso! Usuário cadastrado, empresa criada e autenticado automaticamente."
                };

                _logger.LogInformation(
                    "✓✓✓ REGISTRO COMPLETO FINALIZADO COM SUCESSO! ✓✓✓\n" +
                    "Usuário: {Nome} ({UsuarioId})\n" +
                    "Empresa: {EmpresaNome} ({EmpresaId})\n" +
                    "Token gerado com validade até: {DataExpiracao}",
                    response.Nome,
                    response.UsuarioId,
                    response.EmpresaNome,
                    response.EmpresaId,
                    response.DataExpiracao
                );

                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    "Validação falhou durante registro completo: {Mensagem}. Revertendo operações...",
                    ex.Message
                );

                await ReverterOperacoesAsync(usuarioCriado, empresaCriada, vinculoCriado);

                throw new InvalidOperationException($"Erro de validação: {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    "Dados inválidos durante registro completo: {Mensagem}. Revertendo operações...",
                    ex.Message
                );

                await ReverterOperacoesAsync(usuarioCriado, empresaCriada, vinculoCriado);

                throw new ArgumentException($"Dados inválidos: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado durante registro completo para {Email}. Revertendo operações...",
                    model.Email
                );

                await ReverterOperacoesAsync(usuarioCriado, empresaCriada, vinculoCriado);

                throw new ApplicationException(
                    $"Erro ao realizar registro completo: {ex.Message}. Todas as operações foram revertidas.",
                    ex
                );
            }
        }

        #endregion

        #region Métodos Privados

        /// <summary>
        /// Reverte todas as operações realizadas em caso de erro (rollback manual)
        /// </summary>
        private async Task ReverterOperacoesAsync(
            Usuario? usuarioCriado,
            Empresa? empresaCriada,
            UsuarioEmpresa? vinculoCriado)
        {
            try
            {
                // Reverter na ordem inversa da criação

                // 1. Remover vínculo (se foi criado)
                if (vinculoCriado != null)
                {
                    _logger.LogInformation(
                        "Revertendo: Removendo vínculo usuário-empresa {VinculoId}",
                        vinculoCriado.Id
                    );
                    await _usuarioEmpresaRepository.ExcluirPorIdAsync(vinculoCriado.Id);
                }

                // 2. Remover empresa (se foi criada)
                if (empresaCriada != null)
                {
                    _logger.LogInformation(
                        "Revertendo: Removendo empresa {EmpresaId}",
                        empresaCriada.Id
                    );
                    await _empresaRepository.ExcluirPorIdAsync(empresaCriada.Id);
                }

                // 3. Remover usuário (se foi criado)
                if (usuarioCriado != null)
                {
                    _logger.LogInformation(
                        "Revertendo: Removendo usuário {UsuarioId}",
                        usuarioCriado.Id
                    );
                    await _usuarioRepository.ExcluirPorIdAsync(usuarioCriado.Id);
                }

                _logger.LogInformation("✓ Rollback concluído com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ERRO CRÍTICO: Falha ao reverter operações. Pode haver dados inconsistentes no banco!"
                );
                // Não lançar exceção aqui para não mascarar o erro original
            }
        }

        #endregion
    }
}
