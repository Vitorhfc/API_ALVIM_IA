using System.ComponentModel.DataAnnotations;

namespace Shared.Classes.Model
{
    /// <summary>
    /// Modelo para registro completo de usuário + empresa em uma única requisição
    /// </summary>
    public class RegistroCompletoModel
    {
        // ==================== DADOS DO USUÁRIO ====================

        [Required(ErrorMessage = "Nome do usuário é obrigatório")]
        [StringLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
        public string Nome { get; set; } = "";

        [Required(ErrorMessage = "Email do usuário é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(100, ErrorMessage = "Email deve ter no máximo 100 caracteres")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "CPF é obrigatório")]
        [StringLength(14, ErrorMessage = "CPF deve ter no máximo 14 caracteres")]
        public string Cpf { get; set; } = "";

        [Required(ErrorMessage = "Celular é obrigatório")]
        [StringLength(15, ErrorMessage = "Celular deve ter no máximo 15 caracteres")]
        public string Celular { get; set; } = "";

        [Required(ErrorMessage = "Senha é obrigatória")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Senha deve ter entre 6 e 100 caracteres")]
        public string Senha { get; set; } = "";

        [Required(ErrorMessage = "Data de nascimento é obrigatória")]
        public DateTime DtaNascimento { get; set; }

        // ==================== DADOS DA EMPRESA ====================

        [Required(ErrorMessage = "Razão Social é obrigatória")]
        [StringLength(200, ErrorMessage = "Razão Social deve ter no máximo 200 caracteres")]
        public string RazaoSocial { get; set; } = "";

        [StringLength(200, ErrorMessage = "Nome Fantasia deve ter no máximo 200 caracteres")]
        public string? NomeFantasia { get; set; }

        [Required(ErrorMessage = "CNPJ é obrigatório")]
        [StringLength(18, ErrorMessage = "CNPJ deve ter no máximo 18 caracteres")]
        public string CNPJ { get; set; } = "";

        [Required(ErrorMessage = "Email da empresa é obrigatório")]
        [EmailAddress(ErrorMessage = "Email da empresa inválido")]
        [StringLength(100, ErrorMessage = "Email da empresa deve ter no máximo 100 caracteres")]
        public string EmailEmpresa { get; set; } = "";

        // ==================== CONFIGURAÇÕES OPCIONAIS WAHA ====================

        [StringLength(100, ErrorMessage = "SessionName WAHA deve ter no máximo 100 caracteres")]
        public string? WahaSessionName { get; set; }

        [StringLength(20, ErrorMessage = "Número WhatsApp deve ter no máximo 20 caracteres")]
        public string? WahaNumeroWhatsApp { get; set; }
    }

    /// <summary>
    /// Modelo de resposta do registro completo (usuário cadastrado + empresa criada + token de autenticação)
    /// </summary>
    public class RegistroCompletoResponseModel
    {
        public string UsuarioId { get; set; } = "";
        public string Nome { get; set; } = "";
        public string Email { get; set; } = "";
        public string EmpresaId { get; set; } = "";
        public string EmpresaNome { get; set; } = "";
        public string CNPJ { get; set; } = "";
        public string Token { get; set; } = "";
        public DateTime DataExpiracao { get; set; }
        public IEnumerable<EmpresaVinculadaModel> Empresas { get; set; } = new List<EmpresaVinculadaModel>();
        public string Mensagem { get; set; } = "";
    }
}
