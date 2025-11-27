using Admin_Repository.Repositorio.Interface;
using Admin_Service.Service.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.Model;
using Shared.Services.Interface;
using Shared.Templates;
using Shared.Utils;
using Shared.Utils.Criptografia;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Admin_Service.Service
{
    public class AutenticacaoService : IAutenticacaoService
    {
        #region Campos

        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioEmpresaRepository _usuarioEmpresaRepository;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<AutenticacaoService> _logger;
        private const int TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS = 10;
        private const int TEMPO_EXPIRACAO_TOKEN_JWT_HORAS = 24;

        #endregion

        #region Construtor

        public AutenticacaoService(
            IUsuarioRepository usuarioRepository,
            IUsuarioEmpresaRepository usuarioEmpresaRepository,
            IEmpresaRepository empresaRepository,
            IConfiguration configuration,
            IEmailService emailService,
            IWhatsAppService whatsAppService,
            ILogger<AutenticacaoService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _usuarioEmpresaRepository = usuarioEmpresaRepository;
            _empresaRepository = empresaRepository;
            _configuration = configuration;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        #endregion

        #region Métodos Públicos

        public async Task<LoginResponseModel> AutenticarUsuarioAsync(LoginModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Senha))
                throw new ArgumentException("Email e senha são obrigatórios");

            var usuario = await _usuarioRepository.BuscarPorEmailAsync(model.Email);

            if (usuario == null)
                throw new UnauthorizedAccessException("Email ou senha inválidos");

            var senhaValida = CriptografiaUtils.ValidarSenha(model.Senha, usuario.Senha);

            if (!senhaValida)
                throw new UnauthorizedAccessException("Email ou senha inválidos");

            var response = new LoginResponseModel
            {
                UsuarioId = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                RequerValidacaoDuasEtapas = true,
                TemEmail = !string.IsNullOrWhiteSpace(usuario.Email),
                TemWhatsApp = !string.IsNullOrWhiteSpace(usuario.Celular)
            };

            // Gera e envia o token 2FA automaticamente
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
                        // Obter configurações globais WAHA do appsettings
                        var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                        var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                        // Buscar SessionName da empresa do usuário
                        var empresas = await _usuarioEmpresaRepository.BuscarPorUsuarioIdAsync(usuario.Id);
                        var primeiraEmpresa = empresas.FirstOrDefault();

                        if (primeiraEmpresa != null)
                        {
                            var empresa = await _empresaRepository.BuscarPorIdAsync(primeiraEmpresa.EmpresaId);

                            if (empresa != null &&
                                !string.IsNullOrWhiteSpace(empresa.WahaSessionName) &&
                                !string.IsNullOrWhiteSpace(wahaApiUrl) &&
                                !string.IsNullOrWhiteSpace(wahaApiKey))
                            {
                                var mensagemWhatsApp = TemplatesMensagens.WhatsAppCodigo2FA(
                                    usuario.Nome,
                                    token,
                                    TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS
                                );

                                var resultadoWhatsApp = await _whatsAppService.EnviarTextoAsync(
                                    usuario.Celular,
                                    mensagemWhatsApp,
                                    empresa.WahaSessionName,
                                    wahaApiUrl,
                                    wahaApiKey
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
                            else
                            {
                                _logger.LogWarning(
                                    "Configuração WAHA não encontrada. Session: {Session}, ApiUrl: {ApiUrl}",
                                    empresa?.WahaSessionName ?? "N/A",
                                    wahaApiUrl ?? "N/A"
                                );
                            }
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
                        // Obter configurações globais WAHA do appsettings
                        var wahaApiUrl = _configuration["WAHASettings:ApiUrl"];
                        var wahaApiKey = _configuration["WAHASettings:ApiKey"];

                        // Buscar SessionName da empresa do usuário
                        var empresas = await _usuarioEmpresaRepository.BuscarPorUsuarioIdAsync(usuario.Id);
                        var primeiraEmpresa = empresas.FirstOrDefault();

                        if (primeiraEmpresa != null)
                        {
                            var empresa = await _empresaRepository.BuscarPorIdAsync(primeiraEmpresa.EmpresaId);

                            if (empresa != null &&
                                !string.IsNullOrWhiteSpace(empresa.WahaSessionName) &&
                                !string.IsNullOrWhiteSpace(wahaApiUrl) &&
                                !string.IsNullOrWhiteSpace(wahaApiKey))
                            {
                                var mensagemWhatsApp = TemplatesMensagens.WhatsAppCodigo2FA(
                                    usuario.Nome,
                                    token,
                                    TEMPO_EXPIRACAO_TOKEN_2FA_MINUTOS
                                );

                                var resultadoWhatsApp = await _whatsAppService.EnviarTextoAsync(
                                    usuario.Celular,
                                    mensagemWhatsApp,
                                    empresa.WahaSessionName,
                                    wahaApiUrl,
                                    wahaApiKey
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
                            else
                            {
                                _logger.LogWarning(
                                    "Configuração WAHA não encontrada. Session: {Session}, ApiUrl: {ApiUrl}",
                                    empresa?.WahaSessionName ?? "N/A",
                                    wahaApiUrl ?? "N/A"
                                );
                            }
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

        public async Task<ValidarToken2FAResponseModel> ValidarToken2FAAsync(ValidarToken2FAModel model)
        {
            var usuario = await _usuarioRepository.BuscarPorIdAsync(model.UsuarioId);

            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado");

            if (!usuario.FlgAtivo)
                throw new UnauthorizedAccessException("Usuário inativo");

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

        public async Task<AutenticacaoCompletaResponseModel> SelecionarEmpresaAsync(SelecionarEmpresaModel model)
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

            var token = GerarTokenJWT(usuario);
            var empresasVinculadas = await ObterEmpresasVinculadasAsync(usuario.Id);

            return new AutenticacaoCompletaResponseModel
            {
                UsuarioId = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Token = token,
                DataExpiracao = DateTime.Now.AddHours(TEMPO_EXPIRACAO_TOKEN_JWT_HORAS),
                Empresas = empresasVinculadas
            };
        }

        public async Task<AutenticacaoCompletaResponseModel> LoginUsuarioAsync(LoginUsuarioModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UsuarioId))
                throw new ArgumentException("ID do usuário é obrigatório");

            // Busca o usuário
            var usuario = await _usuarioRepository.BuscarPorIdAsync(model.UsuarioId);

            if (usuario == null)
                throw new KeyNotFoundException("Usuário não encontrado");

            if (!usuario.FlgAtivo)
                throw new UnauthorizedAccessException("Usuário inativo");

            // Atualiza último acesso
            usuario.DtaUltimoAcesso = DateTime.Now;
            await _usuarioRepository.EditarAsync(usuario);

            // Gera token JWT e busca empresas vinculadas
            var token = GerarTokenJWT(usuario);
            var empresasVinculadas = await ObterEmpresasVinculadasAsync(usuario.Id);

            _logger.LogInformation(
                "Login direto realizado com sucesso para o usuário {UsuarioId} - {Nome}",
                usuario.Id,
                usuario.Nome
            );

            return new AutenticacaoCompletaResponseModel
            {
                UsuarioId = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Token = token,
                DataExpiracao = DateTime.Now.AddHours(TEMPO_EXPIRACAO_TOKEN_JWT_HORAS),
                Empresas = empresasVinculadas
            };
        }

        public async Task<AutenticacaoCompletaResponseModel> LoginEmpresaAsync(LoginEmpresaModel model)
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

            // Gera token JWT e busca empresas vinculadas
            var token = GerarTokenJWT(usuario);
            var empresasVinculadas = await ObterEmpresasVinculadasAsync(usuario.Id);

            _logger.LogInformation(
                "Login direto por empresa realizado com sucesso. EmpresaId: {EmpresaId}, UsuarioId: {UsuarioId} - {Nome}",
                model.EmpresaId,
                usuario.Id,
                usuario.Nome
            );

            return new AutenticacaoCompletaResponseModel
            {
                UsuarioId = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Token = token,
                DataExpiracao = DateTime.Now.AddHours(TEMPO_EXPIRACAO_TOKEN_JWT_HORAS),
                Empresas = empresasVinculadas
            };
        }

        #endregion

        #region Métodos Privados

        private string GerarTokenJWT(Usuario usuario)
        {
            var key = _configuration["JwtSettings:Secret"];

            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("JwtSettings:Secret não configurado");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new Claim(ClaimTypes.Email, usuario.Email),
        new Claim(ClaimTypes.Name, usuario.Nome)
    };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Emissor"],
                audience: _configuration["JwtSettings:Audiencia"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(
                    Convert.ToDouble(_configuration["JwtSettings:ExpiracaoHoras"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<IEnumerable<EmpresaVinculadaModel>> ObterEmpresasVinculadasAsync(string usuarioId)
        {
            var vinculos = await _usuarioEmpresaRepository.BuscarPorUsuarioIdAsync(usuarioId);
            var empresasVinculadas = new List<EmpresaVinculadaModel>();

            foreach (var vinculo in vinculos)
            {
                var empresa = await _empresaRepository.BuscarPorIdAsync(vinculo.EmpresaId);

                if (empresa != null)
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

        #endregion
    }
}