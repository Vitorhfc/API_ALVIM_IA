using ADM.Controllers.Base;
using Admin_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Model;

namespace ADM.Controllers
{
    /// <summary>
    /// Controller responsável pelo registro completo de usuário + empresa + autenticação
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class RegistroCompletoController : ControllerBaseComplemento<RegistroCompletoController>
    {
        #region Campos

        private readonly IRegistroCompletoService _registroCompletoService;

        #endregion

        #region Construtor

        public RegistroCompletoController(
            IRegistroCompletoService registroCompletoService,
            ILogADMService logADMService,
            ILogger<RegistroCompletoController> logger)
            : base(logADMService, logger)
        {
            _registroCompletoService = registroCompletoService;
        }

        #endregion

        #region Endpoints

        /// <summary>
        /// Endpoint EXTREMAMENTE COMPLEXO que realiza:
        /// 1. Cadastro de usuário
        /// 2. Cadastro de empresa
        /// 3. Relacionamento usuário-empresa (usuário como administrador)
        /// 4. Autenticação automática do usuário
        /// 5. Retorno do token de autenticação
        ///
        /// TUDO EM UMA ÚNICA TRANSAÇÃO!
        /// </summary>
        /// <param name="model">Dados completos do usuário e empresa</param>
        /// <returns>Usuário cadastrado, empresa criada e token de autenticação</returns>
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarUsuarioEEmpresa([FromBody] RegistroCompletoModel model)
        {
            try
            {
                // Validar ModelState
                if (!ModelState.IsValid)
                {
                    var erros = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    await LogInfoAsync(
                        $"Tentativa de registro completo com dados inválidos: {string.Join(", ", erros)}",
                        nameof(RegistrarUsuarioEEmpresa)
                    );

                    return Erro($"Dados inválidos: {string.Join("; ", erros)}");
                }

                await LogInfoAsync(
                    $"Iniciando registro completo para usuário {model.Nome} e empresa {model.RazaoSocial}",
                    nameof(RegistrarUsuarioEEmpresa)
                );

                // Executar registro completo
                var resultado = await _registroCompletoService.RegistrarUsuarioEEmpresaAsync(model);

                // Registrar ação completa
                await RegistraAcaoAsync(
                    "Registro Completo (Usuário + Empresa + Autenticação)",
                    "Novo registro",
                    SerializarParaLog(new
                    {
                        Usuario = new
                        {
                            resultado.UsuarioId,
                            resultado.Nome,
                            resultado.Email
                        },
                        Empresa = new
                        {
                            resultado.EmpresaId,
                            resultado.EmpresaNome,
                            resultado.CNPJ
                        },
                        TokenGerado = "Sim",
                        DataExpiracao = resultado.DataExpiracao
                    }),
                    $"Registro completo realizado com sucesso: " +
                    $"Usuário '{resultado.Nome}' (ID: {resultado.UsuarioId}), " +
                    $"Empresa '{resultado.EmpresaNome}' (ID: {resultado.EmpresaId}), " +
                    $"Token gerado com validade até {resultado.DataExpiracao:dd/MM/yyyy HH:mm:ss}"
                );

                // Retornar sucesso
                return Sucesso(
                    new
                    {
                        // Dados do usuário
                        usuario = new
                        {
                            id = resultado.UsuarioId,
                            nome = resultado.Nome,
                            email = resultado.Email
                        },
                        // Dados da empresa
                        empresa = new
                        {
                            id = resultado.EmpresaId,
                            nome = resultado.EmpresaNome,
                            cnpj = resultado.CNPJ
                        },
                        // Dados de autenticação
                        autenticacao = new
                        {
                            token = resultado.Token,
                            dataExpiracao = resultado.DataExpiracao,
                            empresasVinculadas = resultado.Empresas
                        },
                        mensagem = resultado.Mensagem
                    },
                    "🎉 Registro completo realizado com sucesso! Bem-vindo(a)!"
                );
            }
            catch (InvalidOperationException ex)
            {
                await LogInfoAsync(
                    $"Validação falhou no registro completo: {ex.Message}",
                    nameof(RegistrarUsuarioEEmpresa)
                );

                return Erro($"Erro de validação: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                await LogInfoAsync(
                    $"Dados inválidos no registro completo: {ex.Message}",
                    nameof(RegistrarUsuarioEEmpresa)
                );

                return Erro($"Dados inválidos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(
                    ex,
                    nameof(RegistrarUsuarioEEmpresa),
                    $"Email: {model.Email}, Empresa: {model.RazaoSocial}"
                );
            }
        }

        /// <summary>
        /// Endpoint de teste/health check
        /// </summary>
        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new
            {
                servico = "Registro Completo API",
                status = "Operacional",
                timestamp = DateTime.Now,
                descricao = "Endpoint para registro completo de usuário + empresa + autenticação em uma única requisição"
            });
        }

        #endregion
    }
}
