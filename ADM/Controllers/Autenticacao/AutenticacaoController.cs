using ADM.Controllers.Base;
using Admin_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Model;

namespace ADM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacaoController : ControllerBaseComplemento<AutenticacaoController>
    {
        #region Campos

        private readonly IAutenticacaoService _autenticacaoService;

        #endregion

        #region Construtor

        public AutenticacaoController(
            IAutenticacaoService autenticacaoService,
            ILogADMService logADMService,
            ILogger<AutenticacaoController> logger)
            : base(logADMService, logger)
        {
            _autenticacaoService = autenticacaoService;
        }

        #endregion

        #region Endpoints

        /// <summary>
        /// Etapa 1: Autentica o usuário e envia o token 2FA automaticamente
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Autenticar([FromBody] LoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Erro("Dados de login inválidos");

                var resultado = await _autenticacaoService.AutenticarUsuarioAsync(model);

                var tipoValidacao = model.TipoValidacao == TipoValidacaoDuasEtapas.Email ? "Email" : "WhatsApp";

                await LogInfoAsync(
                    $"Usuário {model.Email} autenticado. Token 2FA enviado via {tipoValidacao}",
                    nameof(Autenticar));

                return Sucesso(resultado, resultado.Mensagem);
            }
            catch (UnauthorizedAccessException ex)
            {
                await LogInfoAsync($"Tentativa de login com credenciais inválidas: {model.Email}", nameof(Autenticar));
                return Erro(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return Erro(ex.Message);
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(Autenticar), $"Email: {model.Email}");
            }
        }

        /// <summary>
        /// Etapa 2: Valida o token 2FA e retorna lista de empresas do usuário
        /// </summary>
        [HttpPost("validar-token-2fa")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarToken2FA([FromBody] ValidarToken2FAModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Erro("Dados inválidos");

                var resultado = await _autenticacaoService.ValidarToken2FAAsync(model);

                await LogInfoAsync(
                    $"Token 2FA validado com sucesso para usuário {model.UsuarioId}",
                    nameof(ValidarToken2FA));

                return Sucesso(resultado, "Token validado com sucesso. Selecione a empresa");
            }
            catch (UnauthorizedAccessException ex)
            {
                await LogInfoAsync($"Tentativa de validação 2FA com token inválido: {model.UsuarioId}", nameof(ValidarToken2FA));
                return Erro(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return Erro(ex.Message);
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ValidarToken2FA), $"UsuarioId: {model.UsuarioId}");
            }
        }

        /// <summary>
        /// Etapa 3: Seleciona a empresa e completa o login
        /// </summary>
        [HttpPost("selecionar-empresa")]
        [AllowAnonymous]
        public async Task<IActionResult> SelecionarEmpresa([FromBody] SelecionarEmpresaModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Erro("Dados inválidos");

                var resultado = await _autenticacaoService.SelecionarEmpresaAsync(model);

                await RegistraAcaoAsync(
                    "Login Completo",
                    $"UsuarioId: {model.UsuarioId}, EmpresaId: {model.EmpresaId}",
                    SerializarParaLog(resultado),
                    $"Login completo para usuário {resultado.Nome} na empresa {model.EmpresaId}");

                return Sucesso(resultado, "Login realizado com sucesso");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Erro(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return Erro(ex.Message);
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(SelecionarEmpresa), SerializarParaLog(model));
            }
        }

        /// <summary>
        /// Login direto com ID do usuário (retorna lista de empresas)
        /// </summary>
        [HttpPost("login-usuario")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginUsuario([FromBody] LoginUsuarioModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Erro("Dados inválidos");

                var resultado = await _autenticacaoService.LoginUsuarioAsync(model);

                await RegistraAcaoAsync(
                    "Login Direto por Usuário",
                    $"UsuarioId: {model.UsuarioId}",
                    SerializarParaLog(resultado),
                    $"Login direto realizado para usuário {resultado.Nome}. Retornando {resultado.Empresas.Count()} empresa(s) vinculada(s)");

                return Sucesso(resultado, "Login realizado com sucesso");
            }
            catch (KeyNotFoundException ex)
            {
                await LogInfoAsync($"Tentativa de login direto com usuário inexistente: {model.UsuarioId}", nameof(LoginUsuario));
                return Erro(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Erro(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return Erro(ex.Message);
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(LoginUsuario), $"UsuarioId: {model.UsuarioId}");
            }
        }

        /// <summary>
        /// Login direto com ID da empresa (retorna lista de empresas do primeiro administrador)
        /// </summary>
        [HttpPost("login-empresa")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginEmpresa([FromBody] LoginEmpresaModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Erro("Dados inválidos");

                var resultado = await _autenticacaoService.LoginEmpresaAsync(model);

                await RegistraAcaoAsync(
                    "Login Direto por Empresa",
                    $"EmpresaId: {model.EmpresaId}",
                    SerializarParaLog(resultado),
                    $"Login direto realizado para empresa {model.EmpresaId}. Usuário logado: {resultado.Nome}. Retornando {resultado.Empresas.Count()} empresa(s) vinculada(s)");

                return Sucesso(resultado, "Login realizado com sucesso");
            }
            catch (KeyNotFoundException ex)
            {
                await LogInfoAsync($"Tentativa de login direto com empresa inexistente ou sem administrador: {model.EmpresaId}", nameof(LoginEmpresa));
                return Erro(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Erro(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return Erro(ex.Message);
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(LoginEmpresa), $"EmpresaId: {model.EmpresaId}");
            }
        }

        #endregion
    }
}
