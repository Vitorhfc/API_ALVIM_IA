using System.ComponentModel.DataAnnotations;

namespace Shared.Classes.Model
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email é obrigatório")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Senha { get; set; }

        [Required(ErrorMessage = "Tipo de validação é obrigatório")]
        public TipoValidacaoDuasEtapas TipoValidacao { get; set; }
    }

    public class LoginResponseModel
    {
        public string UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public bool RequerValidacaoDuasEtapas { get; set; }
        public bool TemEmail { get; set; }
        public bool TemWhatsApp { get; set; }
        public string DestinoEnvio { get; set; }
        public string Mensagem { get; set; }
    }

    public class SolicitarValidacaoDuasEtapasModel
    {
        public string UsuarioId { get; set; }
        public TipoValidacaoDuasEtapas TipoValidacao { get; set; }
    }

    public class ValidacaoDuasEtapasResponseModel
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; }
        public string DestinoEnvio { get; set; }
    }

    public class ConfirmarValidacaoDuasEtapasClientModel
    {
        [Required(ErrorMessage = "UsuarioId � obrigat�rio")]
        public string UsuarioId { get; set; }

        [Required(ErrorMessage = "EmpresaId � obrigat�rio")]
        public string EmpresaId { get; set; }

        [Required(ErrorMessage = "Token � obrigat�rio")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Token deve ter 6 d�gitos")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Tipo de valida��o � obrigat�rio")]
        public TipoValidacaoDuasEtapas TipoValidacao { get; set; }
    }

    public class AutenticacaoClientCompletaResponseModel
    {
        public string UsuarioId { get; set; }
        public string EmpresaId { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public bool FlgAdministrador { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime DataExpiracao { get; set; }
        public DateTime ProximoReloginObrigatorio { get; set; }
        public string? TokenGoogle { get; set; }
        public bool flgEmpresaPier { get; set; } = false;
    }

    public class RefreshTokenModel
    {
        [Required(ErrorMessage = "RefreshToken � obrigat�rio")]
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenResponseModel
    {
        public string UsuarioId { get; set; }
        public string EmpresaId { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime DataExpiracao { get; set; }
        public DateTime ProximoReloginObrigatorio { get; set; }
    }

    public class ConfirmarValidacaoDuasEtapasModel
    {
        public string UsuarioId { get; set; }
        public string Token { get; set; }
        public TipoValidacaoDuasEtapas TipoValidacao { get; set; }
    }

    public class AutenticacaoCompletaResponseModel
    {
        public string UsuarioId { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime DataExpiracao { get; set; }
        public IEnumerable<EmpresaVinculadaModel> Empresas { get; set; }
    }

    public class EmpresaVinculadaModel
    {
        public string EmpresaId { get; set; }
        public string NomeEmpresa { get; set; }
        public bool FlgAdministrador { get; set; }
    }

    public class ValidarToken2FAModel
    {
        [Required(ErrorMessage = "UsuarioId é obrigatório")]
        public string UsuarioId { get; set; }

        [Required(ErrorMessage = "Token é obrigatório")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Token deve ter 6 dígitos")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Tipo de validação é obrigatório")]
        public TipoValidacaoDuasEtapas TipoValidacao { get; set; }
    }

    public class ValidarToken2FAResponseModel
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; }
        public string UsuarioId { get; set; }
        public IEnumerable<EmpresaVinculadaModel> Empresas { get; set; }
    }

    public class SelecionarEmpresaModel
    {
        [Required(ErrorMessage = "UsuarioId é obrigatório")]
        public string UsuarioId { get; set; }

        [Required(ErrorMessage = "EmpresaId é obrigatório")]
        public string EmpresaId { get; set; }
    }

    public class LoginEmpresaModel
    {
        [Required(ErrorMessage = "EmpresaId é obrigatório")]
        public string EmpresaId { get; set; }
    }

    public class LoginUsuarioModel
    {
        [Required(ErrorMessage = "UsuarioId é obrigatório")]
        public string UsuarioId { get; set; }
    }

    public enum TipoValidacaoDuasEtapas
    {
        Email,
        WhatsApp
    }
}