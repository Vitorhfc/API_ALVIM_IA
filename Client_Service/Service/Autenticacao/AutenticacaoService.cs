using Admin_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Model;
using Shared.Services.Interface;
using Shared.Templates;
using Shared.Utils.Criptografia;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Client_Service.Service
{
    public class AutenticacaoClientService : IAutenticacaoClientService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IWhatsAppInternoService _whatsAppInternoService;
        private readonly ILogger<AutenticacaoClientService> _logger;

        private const int TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS = 5;
        private const int TEMPO_EXPIRACAO_TOKEN_JWT_HORAS = 24;
        private const int TEMPO_EXPIRACAO_REFRESH_TOKEN_DIAS = 7;
        private const int DIAS_PARA_RELOGIN_OBRIGATORIO = 2;

        public AutenticacaoClientService(
            IUsuarioRepository usuarioRepository,
            IUsuarioEmpresaRepository usuarioEmpresaRepository,
            IEmpresaRepository empresaRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IConfiguration configuration,
            IEmailService emailService,
            IWhatsAppInternoService whatsAppInternoService,
            ILogger<AutenticacaoClientService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _usuarioEmpresaRepository = usuarioEmpresaRepository;
            _empresaRepository = empresaRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _configuration = configuration;
            _emailService = emailService;
            _whatsAppInternoService = whatsAppInternoService;
            _logger = logger;
        }

        public async Task<LoginResponseModel> AutenticarUsuarioAsync(LoginModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Senha))
                throw new ArgumentException("Email e senha são obrigatórios");

            var usuario = await _usuarioRepository.BuscarPorEmailAsync(model.Email);

            if (usuario == null)
                throw new UnauthorizedAccessException("Email ou senha inválidos");

            if (!usuario.FlgAtivo)
                throw new UnauthorizedAccessException("Usuário inativo");

            var senhaValida = CriptografiaUtils.ValidarSenha(model.Senha, usuario.Senha);

            if (!senhaValida)
                throw new UnauthorizedAccessException("Email ou senha inválidos");

            var response = new LoginResponseModel
            {
                UsuarioId = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                RequerValidacaoDuasEtapas = usuario.FlgAutenticacaoDuasEtapas,
                TemEmail = !string.IsNullOrWhiteSpace(usuario.Email),
                TemWhatsApp = !string.IsNullOrWhiteSpace(usuario.Celular)
            };

            var token = GerarTokenNumerico();
            var dataExpiracao = DateTime.Now.AddMinutes(TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS);

            switch (model.TipoValidacao)
            {
                case TipoValidacaoDuasEtapas.Email:
                    if (string.IsNullOrWhiteSpace(usuario.Email))
                        throw new ArgumentException("Usuário não possui email cadastrado");

                    usuario.TokenEmailConfirmacao = token;
                    usuario.DtaTokenEmailGerado = dataExpiracao;
                    await _usuarioRepository.EditarAsync(usuario);

                    // Enviar email com o token
                    try
                    {
                        var corpoEmail = TemplatesMensagens.EmailCodigo2FA(
                            usuario.Nome,
                            token,
                            TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS
                        );

                        var resultadoEmail = await _emailService.EnviarEmailHTMLAsync(
                            usuario.Email,
                            TemplatesMensagens.EmailCodigo2FAAssunto,
                            corpoEmail
                        );

                        if (!resultadoEmail.Sucesso)
                        {
                            _logger.LogWarning(
                                "Falha ao enviar email 2FA para {Email}: {Erro}",
                                usuario.Email,
                                resultadoEmail.Erro
                            );
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Email 2FA enviado com sucesso para {Email}",
                                usuario.Email
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar email 2FA para {Email}", usuario.Email);
                        // Não interrompe o fluxo - o token foi gerado e salvo
                    }

                    response.DestinoEnvio = MascararEmail(usuario.Email);
                    response.Mensagem = "Token de validação enviado para seu email";
                    break;

                case TipoValidacaoDuasEtapas.WhatsApp:
                    if (string.IsNullOrWhiteSpace(usuario.Celular))
                        throw new ArgumentException("Usuário não possui celular cadastrado");

                    usuario.TokenCelularConfirmacao = token;
                    usuario.DtaTokenCelularGerado = dataExpiracao;
                    await _usuarioRepository.EditarAsync(usuario);

                    // Enviar WhatsApp com o token
                    try
                    {
                        var mensagemWhatsApp = TemplatesMensagens.WhatsAppCodigo2FA(
                            usuario.Nome,
                            token,
                            TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS
                        );

                        var resultadoWhatsApp = await _whatsAppInternoService.EnviarTextoAsync(
                            usuario.Celular,
                            mensagemWhatsApp
                        );

                        if (!resultadoWhatsApp.Sucesso)
                        {
                            _logger.LogWarning(
                                "Falha ao enviar WhatsApp 2FA para {Celular}: {Erro}",
                                usuario.Celular,
                                resultadoWhatsApp.Erro
                            );
                        }
                        else
                        {
                            _logger.LogInformation(
                                "WhatsApp 2FA enviado com sucesso para {Celular}",
                                usuario.Celular
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar WhatsApp 2FA para {Celular}", usuario.Celular);
                        // Não interrompe o fluxo - o token foi gerado e salvo
                    }

                    response.DestinoEnvio = MascararCelular(usuario.Celular);
                    response.Mensagem = "Token de validação enviado para seu WhatsApp";
                    break;

                default:
                    throw new ArgumentException("Tipo de validação inválido");
            }


            return response;
        }

        public async Task<ValidacaoDuasEtapasResponseModel> SolicitarValidacaoDuasEtapasAsync(SolicitarValidacaoDuasEtapasModel model)
        {
            var usuario = await _usuarioRepository.BuscarPorIdAsync(model.UsuarioId);

            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado");

            if (!usuario.FlgAutenticacaoDuasEtapas)
                throw new InvalidOperationException("Autenticação de duas etapas desabilitada para este usuário");

            var token = GerarTokenNumerico();
            var dataExpiracao = DateTime.Now.AddMinutes(TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS);

            switch (model.TipoValidacao)
            {
                case TipoValidacaoDuasEtapas.Email:
                    usuario.TokenEmailConfirmacao = token;
                    usuario.DtaTokenEmailGerado = dataExpiracao;
                    await _usuarioRepository.EditarAsync(usuario);

                    // Enviar email com o token
                    try
                    {
                        var corpoEmail = TemplatesMensagens.EmailCodigo2FA(
                            usuario.Nome,
                            token,
                            TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS
                        );

                        var resultadoEmail = await _emailService.EnviarEmailHTMLAsync(
                            usuario.Email,
                            TemplatesMensagens.EmailCodigo2FAAssunto,
                            corpoEmail
                        );

                        if (!resultadoEmail.Sucesso)
                        {
                            _logger.LogWarning(
                                "Falha ao enviar email 2FA para {Email}: {Erro}",
                                usuario.Email,
                                resultadoEmail.Erro
                            );
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Email 2FA enviado com sucesso para {Email}",
                                usuario.Email
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar email 2FA para {Email}", usuario.Email);
                        // Não interrompe o fluxo - o token foi gerado e salvo
                    }

                    return new ValidacaoDuasEtapasResponseModel
                    {
                        Sucesso = true,
                        Mensagem = "Token de validação enviado para seu email",
                        DestinoEnvio = MascararEmail(usuario.Email)
                    };

                case TipoValidacaoDuasEtapas.WhatsApp:
                    if (string.IsNullOrWhiteSpace(usuario.Celular))
                        throw new ArgumentException("Usuário não possui celular cadastrado");

                    usuario.TokenCelularConfirmacao = token;
                    usuario.DtaTokenCelularGerado = dataExpiracao;
                    await _usuarioRepository.EditarAsync(usuario);

                    // Enviar WhatsApp com o token
                    try
                    {
                        var mensagemWhatsApp = TemplatesMensagens.WhatsAppCodigo2FA(
                            usuario.Nome,
                            token,
                            TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS
                        );

                        var resultadoWhatsApp = await _whatsAppInternoService.EnviarTextoAsync(
                            usuario.Celular,
                            mensagemWhatsApp
                        );

                        if (!resultadoWhatsApp.Sucesso)
                        {
                            _logger.LogWarning(
                                "Falha ao enviar WhatsApp 2FA para {Celular}: {Erro}",
                                usuario.Celular,
                                resultadoWhatsApp.Erro
                            );
                        }
                        else
                        {
                            _logger.LogInformation(
                                "WhatsApp 2FA enviado com sucesso para {Celular}",
                                usuario.Celular
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar WhatsApp 2FA para {Celular}", usuario.Celular);
                        // Não interrompe o fluxo - o token foi gerado e salvo
                    }

                    return new ValidacaoDuasEtapasResponseModel
                    {
                        Sucesso = true,
                        Mensagem = "Token de validação enviado para seu WhatsApp",
                        DestinoEnvio = MascararCelular(usuario.Celular)
                    };

                default:
                    throw new ArgumentException("Tipo de validação inválido");
            }
        }

        public async Task<AutenticacaoClientCompletaResponseModel> ConfirmarValidacaoDuasEtapasAsync(ConfirmarValidacaoDuasEtapasClientModel model)
        {
            var usuario = await _usuarioRepository.BuscarPorIdAsync(model.UsuarioId);

            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado");

            if (!usuario.FlgAtivo)
                throw new UnauthorizedAccessException("Usuário inativo");

            var empresa = await _empresaRepository.BuscarPorIdAsync(model.EmpresaId);

            if (empresa == null)
                throw new KeyNotFoundException("Empresa não encontrada");

            if (!empresa.FlgAtivo)
                throw new UnauthorizedAccessException("Empresa inativa");

            var usuarioEmpresa = await _usuarioEmpresaRepository.BuscarPorUsuarioEmpresaAsync(model.UsuarioId, model.EmpresaId);

            if (usuarioEmpresa == null)
                throw new UnauthorizedAccessException("Usuário não vinculado a esta empresa");

            if (!usuarioEmpresa.FlgAtivo)
                throw new UnauthorizedAccessException("Vínculo usuário-empresa inativo");

            if (usuario.FlgAutenticacaoDuasEtapas)
            {
                bool tokenValido = false;
                DateTime? dataExpiracao = null;

                switch (model.TipoValidacao)
                {
                    case TipoValidacaoDuasEtapas.Email:
                        tokenValido = usuario.TokenEmailConfirmacao == model.Token;
                        dataExpiracao = usuario.DtaTokenEmailGerado;
                        break;

                    case TipoValidacaoDuasEtapas.WhatsApp:
                        tokenValido = usuario.TokenCelularConfirmacao == model.Token;
                        dataExpiracao = usuario.DtaTokenCelularGerado;
                        break;

                    default:
                        throw new ArgumentException("Tipo de validação inválido");
                }

                if (!tokenValido)
                    throw new UnauthorizedAccessException("Token inválido");

                if (dataExpiracao == null || dataExpiracao < DateTime.Now)
                    throw new UnauthorizedAccessException("Token expirado");
            }

            usuario.DtaUltimoAcesso = DateTime.Now;
            usuario.TokenEmailConfirmacao = string.Empty;
            usuario.TokenCelularConfirmacao = string.Empty;
            usuario.DtaTokenEmailGerado = null;
            usuario.DtaTokenCelularGerado = null;

            await _usuarioRepository.EditarAsync(usuario);

            var proximoRelogin = DateTime.Now.AddDays(DIAS_PARA_RELOGIN_OBRIGATORIO);
            var token = GerarTokenJWT(usuario, model.EmpresaId, usuarioEmpresa.FlgAdministrador, proximoRelogin);
            var refreshToken = await GerarRefreshTokenAsync(usuario.Id, model.EmpresaId);

            return new AutenticacaoClientCompletaResponseModel
            {
                UsuarioId = usuario.Id,
                EmpresaId = model.EmpresaId,
                Nome = usuario.Nome,
                Email = usuario.Email,
                FlgAdministrador = usuarioEmpresa.FlgAdministrador,
                Token = token,
                RefreshToken = refreshToken,
                DataExpiracao = DateTime.Now.AddHours(TEMPO_EXPIRACAO_TOKEN_JWT_HORAS),
                ProximoReloginObrigatorio = proximoRelogin,
                TokenGoogle = null
            };
        }

        public async Task<ValidarToken2FAResponseModel> ValidarToken2FAAsync(ValidarToken2FAModel model)
        {
            var usuario = await _usuarioRepository.BuscarPorIdAsync(model.UsuarioId);

            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado");

            if (!usuario.FlgAtivo)
                throw new UnauthorizedAccessException("Usuário inativo");

            bool tokenValido = false;
            DateTime? dataExpiracao = null;

            switch (model.TipoValidacao)
            {
                case TipoValidacaoDuasEtapas.Email:
                    tokenValido = usuario.TokenEmailConfirmacao == model.Token;
                    dataExpiracao = usuario.DtaTokenEmailGerado;
                    break;

                case TipoValidacaoDuasEtapas.WhatsApp:
                    tokenValido = usuario.TokenCelularConfirmacao == model.Token;
                    dataExpiracao = usuario.DtaTokenCelularGerado;
                    break;

                default:
                    throw new ArgumentException("Tipo de validação inválido");
            }

            if (!tokenValido)
                throw new UnauthorizedAccessException("Token inválido");

            if (dataExpiracao == null || dataExpiracao < DateTime.Now)
                throw new UnauthorizedAccessException("Token expirado");


            // Limpa os tokens 2FA
            usuario.TokenEmailConfirmacao = string.Empty;
            usuario.TokenCelularConfirmacao = string.Empty;
            usuario.DtaTokenEmailGerado = null;
            usuario.DtaTokenCelularGerado = null;
            await _usuarioRepository.EditarAsync(usuario);

            // Busca empresas vinculadas
            var empresasVinculadas = await ObterEmpresasVinculadasAsync(usuario.Id);

            return new ValidarToken2FAResponseModel
            {
                Sucesso = true,
                Mensagem = "Token validado com sucesso",
                UsuarioId = usuario.Id,
                Empresas = empresasVinculadas
            };
        }

        public async Task<AutenticacaoClientCompletaResponseModel> SelecionarEmpresaAsync(SelecionarEmpresaModel model)
        {
            var usuario = await _usuarioRepository.BuscarPorIdAsync(model.UsuarioId);

            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado");

            if (!usuario.FlgAtivo)
                throw new UnauthorizedAccessException("Usuário inativo");

            var empresa = await _empresaRepository.BuscarPorIdAsync(model.EmpresaId);

            if (empresa == null)
                throw new KeyNotFoundException("Empresa não encontrada");

            if (!empresa.FlgAtivo)
                throw new UnauthorizedAccessException("Empresa inativa");

            var usuarioEmpresa = await _usuarioEmpresaRepository.BuscarPorUsuarioEmpresaAsync(model.UsuarioId, model.EmpresaId);

            if (usuarioEmpresa == null)
                throw new UnauthorizedAccessException("Usuário não vinculado a esta empresa");

            if (!usuarioEmpresa.FlgAtivo)
                throw new UnauthorizedAccessException("Vínculo usuário-empresa inativo");

            usuario.DtaUltimoAcesso = DateTime.Now;
            await _usuarioRepository.EditarAsync(usuario);

            var proximoRelogin = DateTime.Now.AddDays(DIAS_PARA_RELOGIN_OBRIGATORIO);
            var token = GerarTokenJWT(usuario, model.EmpresaId, usuarioEmpresa.FlgAdministrador, proximoRelogin);
            var refreshToken = await GerarRefreshTokenAsync(usuario.Id, model.EmpresaId);

            return new AutenticacaoClientCompletaResponseModel
            {
                UsuarioId = usuario.Id,
                EmpresaId = model.EmpresaId,
                Nome = usuario.Nome,
                Email = usuario.Email,
                FlgAdministrador = usuarioEmpresa.FlgAdministrador,
                Token = token,
                RefreshToken = refreshToken,
                DataExpiracao = DateTime.Now.AddHours(TEMPO_EXPIRACAO_TOKEN_JWT_HORAS),
                ProximoReloginObrigatorio = proximoRelogin,
                TokenGoogle = null
            };
        }

        public async Task<AutenticacaoClientCompletaResponseModel> LoginEmpresaAsync(LoginEmpresaModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EmpresaId))
                throw new ArgumentException("ID da empresa é obrigatório");

            // Busca todos os vínculos de usuário-empresa para a empresa informada
            var vinculos = await _usuarioEmpresaRepository.BuscarPorEmpresaIdAsync(model.EmpresaId);

            // Busca o primeiro administrador ativo
            var vinculoAdministrador = vinculos
                .Where(v => v.FlgAdministrador && v.FlgAtivo)
                .OrderBy(v => v.DtaCadastro)
                .FirstOrDefault();

            if (vinculoAdministrador == null)
                throw new KeyNotFoundException("Nenhum administrador ativo encontrado para esta empresa");

            // Busca o usuário administrador
            var usuario = await _usuarioRepository.BuscarPorIdAsync(vinculoAdministrador.UsuarioId);

            if (usuario == null)
                throw new KeyNotFoundException("Usuário administrador não encontrado");

            if (!usuario.FlgAtivo)
                throw new UnauthorizedAccessException("Usuário administrador inativo");

            var empresa = await _empresaRepository.BuscarPorIdAsync(model.EmpresaId);

            if (empresa == null)
                throw new KeyNotFoundException("Empresa não encontrada");

            if (!empresa.FlgAtivo)
                throw new UnauthorizedAccessException("Empresa inativa");

            // Atualiza último acesso
            usuario.DtaUltimoAcesso = DateTime.Now;
            await _usuarioRepository.EditarAsync(usuario);

            var proximoRelogin = DateTime.Now.AddDays(DIAS_PARA_RELOGIN_OBRIGATORIO);
            var token = GerarTokenJWT(usuario, model.EmpresaId, true, proximoRelogin);
            var refreshToken = await GerarRefreshTokenAsync(usuario.Id, model.EmpresaId);

            return new AutenticacaoClientCompletaResponseModel
            {
                UsuarioId = usuario.Id,
                EmpresaId = model.EmpresaId,
                Nome = usuario.Nome,
                Email = usuario.Email,
                FlgAdministrador = true,
                Token = token,
                RefreshToken = refreshToken,
                DataExpiracao = DateTime.Now.AddHours(TEMPO_EXPIRACAO_TOKEN_JWT_HORAS),
                ProximoReloginObrigatorio = proximoRelogin,
                TokenGoogle = null
            };
        }

        public async Task<RefreshTokenResponseModel> RefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepository.BuscarPorTokenAsync(refreshToken);

            if (token == null)
                throw new UnauthorizedAccessException("Refresh token inválido");

            if (token.FlgRevogado)
                throw new UnauthorizedAccessException("Refresh token revogado");

            if (token.FlgUtilizado)
                throw new UnauthorizedAccessException("Refresh token já utilizado");

            if (token.DtaExpiracao < DateTime.Now)
                throw new UnauthorizedAccessException("Refresh token expirado");

            var usuario = await _usuarioRepository.BuscarPorIdAsync(token.UsuarioId);

            if (usuario == null || !usuario.FlgAtivo)
                throw new UnauthorizedAccessException("Usuário inválido ou inativo");

            var usuarioEmpresa = await _usuarioEmpresaRepository.BuscarPorUsuarioEmpresaAsync(token.UsuarioId, token.EmpresaId);

            if (usuarioEmpresa == null || !usuarioEmpresa.FlgAtivo)
                throw new UnauthorizedAccessException("Vínculo usuário-empresa inválido");

            token.FlgUtilizado = true;
            token.DtaUso = DateTime.Now;
            await _refreshTokenRepository.EditarAsync(token);

            var proximoRelogin = DateTime.Now.AddDays(DIAS_PARA_RELOGIN_OBRIGATORIO);
            var novoToken = GerarTokenJWT(usuario, token.EmpresaId, usuarioEmpresa.FlgAdministrador, proximoRelogin);
            var novoRefreshToken = await GerarRefreshTokenAsync(token.UsuarioId, token.EmpresaId);

            return new RefreshTokenResponseModel
            {
                UsuarioId = usuario.Id,
                EmpresaId = token.EmpresaId,
                Token = novoToken,
                RefreshToken = novoRefreshToken,
                DataExpiracao = DateTime.Now.AddHours(TEMPO_EXPIRACAO_TOKEN_JWT_HORAS),
                ProximoReloginObrigatorio = proximoRelogin
            };
        }

        public async Task InvalidarRefreshTokensAsync(string usuarioId)
        {
            var tokens = await _refreshTokenRepository.BuscarPorUsuarioIdAsync(usuarioId);

            foreach (var token in tokens.Where(t => !t.FlgRevogado && !t.FlgUtilizado))
            {
                token.FlgRevogado = true;
                await _refreshTokenRepository.EditarAsync(token);
            }
        }

        private string GerarTokenJWT(Usuario usuario, string empresaId, bool flgAdministrador, DateTime proximoRelogin)
        {
            var key = _configuration["JwtSettings:Secret"];

            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("JwtSettings:Secret não configurado");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("UsuarioId", usuario.Id),
                new Claim("EmpresaId", empresaId),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim("FlgAdministrador", flgAdministrador.ToString()),
                new Claim("ProximoRelogin", proximoRelogin.ToString("o"))
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Emissor"],
                audience: _configuration["JwtSettings:Audiencia"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(TEMPO_EXPIRACAO_TOKEN_JWT_HORAS),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> GerarRefreshTokenAsync(string usuarioId, string empresaId)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var token = Convert.ToBase64String(randomBytes);

            var refreshToken = new RefreshToken
            {
                UsuarioId = usuarioId,
                EmpresaId = empresaId,
                Token = token,
                DtaCadastro = DateTime.Now,
                DtaExpiracao = DateTime.Now.AddDays(TEMPO_EXPIRACAO_REFRESH_TOKEN_DIAS),
                FlgRevogado = false,
                FlgUtilizado = false
            };

            await _refreshTokenRepository.AdicionarAsync(refreshToken);

            return token;
        }

        private static string GerarTokenNumerico()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private static string MascararEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return "***@***.***";

            var partes = email.Split('@');
            var usuario = partes[0];
            var dominio = partes[1];

            var usuarioMascarado = usuario.Length <= 2
                ? new string('*', usuario.Length)
                : $"{usuario[0]}{new string('*', usuario.Length - 2)}{usuario[^1]}";

            var dominioPartes = dominio.Split('.');
            var dominioMascarado = dominioPartes.Length > 1
                ? $"{dominioPartes[0][0]}{new string('*', dominioPartes[0].Length - 1)}.{dominioPartes[^1]}"
                : new string('*', dominio.Length);

            return $"{usuarioMascarado}@{dominioMascarado}";
        }

        private static string MascararCelular(string celular)
        {
            if (string.IsNullOrWhiteSpace(celular))
                return "(**) *****-****";

            var numeros = new string(celular.Where(char.IsDigit).ToArray());

            if (numeros.Length < 10)
                return "(**) *****-****";

            var ddd = numeros.Substring(0, 2);
            var ultimosDigitos = numeros.Substring(numeros.Length - 4);

            return $"({ddd}) *****-{ultimosDigitos}";
        }

        private async Task<IEnumerable<EmpresaVinculadaModel>> ObterEmpresasVinculadasAsync(string usuarioId)
        {
            var vinculos = await _usuarioEmpresaRepository.BuscarPorUsuarioIdAsync(usuarioId);
            var empresasVinculadas = new List<EmpresaVinculadaModel>();

            foreach (var vinculo in vinculos.Where(v => v.FlgAtivo))
            {
                var empresa = await _empresaRepository.BuscarPorIdAsync(vinculo.EmpresaId);

                if (empresa != null && empresa.FlgAtivo)
                {
                    empresasVinculadas.Add(new EmpresaVinculadaModel
                    {
                        EmpresaId = empresa.Id,
                        NomeEmpresa = empresa.Nome,
                        FlgAdministrador = vinculo.FlgAdministrador
                    });
                }
            }

            return empresasVinculadas;
        }
    }
}