using Client.Controllers.Base;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Model;

namespace Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutenticacaoController : ControllerBaseClient<AutenticacaoController>
    {
        private readonly IAutenticacaoClientService _autenticacaoService;

        public AutenticacaoController(
            IAutenticacaoClientService autenticacaoService,
            ILogClientService logClientService,
            ILogger<AutenticacaoController> logger)
            : base(logClientService, logger)
        {
            _autenticacaoService = autenticacaoService;
        }

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
                return LogErroAsync(ex, nameof(Autenticar), $"Email: {model.Email}");
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
                return LogErroAsync(ex, nameof(ValidarToken2FA), $"UsuarioId: {model.UsuarioId}");
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

                await LogInfoAsync(
                    $"Login completo para usuário {resultado.Nome} na empresa {resultado.EmpresaId}",
                    nameof(SelecionarEmpresa));

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
                return LogErroAsync(ex, nameof(SelecionarEmpresa), SerializarParaLog(model));
            }
        }

        /// <summary>
        /// Processo alternativo: Login direto com ID da empresa (usa primeiro administrador)
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

                await LogInfoAsync(
                    $"Login direto realizado para empresa {model.EmpresaId}. Usuário: {resultado.Nome}",
                    nameof(LoginEmpresa));

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
                return LogErroAsync(ex, nameof(LoginEmpresa), $"EmpresaId: {model.EmpresaId}");
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Erro("Dados inválidos");

                var resultado = await _autenticacaoService.RefreshTokenAsync(model.RefreshToken);

                await LogInfoAsync($"Token renovado para usuário {resultado.UsuarioId}", nameof(RefreshToken));

                return Sucesso(resultado, "Token renovado com sucesso");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Erro(ex.Message);
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(RefreshToken), "Refresh token inválido");
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var usuarioId = HttpContext.Items["UsuarioId"]?.ToString();
                var empresaId = HttpContext.Items["EmpresaId"]?.ToString();

                if (string.IsNullOrEmpty(usuarioId))
                    return Erro("Usuário não identificado");

                await _autenticacaoService.InvalidarRefreshTokensAsync(usuarioId);

                await LogInfoAsync($"Usuário {usuarioId} fez logout", nameof(Logout));

                return Sucesso(null, "Logout realizado com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(Logout));
            }
        }
    }
}
