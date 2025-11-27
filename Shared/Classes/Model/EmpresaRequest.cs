using System.ComponentModel.DataAnnotations;

namespace Shared.Classes.Model
{
    /// <summary>
    /// Model para cadastro de empresa com todas as configurações necessárias
    /// </summary>
    public class CadastrarEmpresaRequest
    {
        [Required(ErrorMessage = "Razão Social é obrigatória")]
        public string RazaoSocial { get; set; }

        public string? NomeFantasia { get; set; }

        [Required(ErrorMessage = "CNPJ é obrigatório")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "CNPJ deve conter 14 dígitos")]
        public string CNPJ { get; set; }

        [EmailAddress(ErrorMessage = "Email inválido")]
        public string? Email { get; set; }

        // Configurações WAHA
        [Required(ErrorMessage = "Número do WhatsApp é obrigatório")]
        public string NumeroWhatsApp { get; set; }

        [Required(ErrorMessage = "Nome da sessão WAHA é obrigatório")]
        public string WahaSessionName { get; set; }

        // Configuração do Banco
        public string? NomeBaseDadosCustomizado { get; set; } // Se não informado, gera automaticamente
    }

    /// <summary>
    /// Response com todas as informações da empresa criada
    /// </summary>
    public class EmpresaCriadaResponse
    {
        public string EmpresaId { get; set; }
        public string RazaoSocial { get; set; }
        public string CNPJ { get; set; }
        public string NumeroWhatsApp { get; set; }
        public string NomeBaseDados { get; set; }
        public string ConnectionString { get; set; }
        public ConfiguracaoInicialResponse ConfiguracaoInicial { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    public class ConfiguracaoInicialResponse
    {
        public string ConfiguracaoIAId { get; set; }
        public bool BancoCriado { get; set; }
        public bool CollectionsCriadas { get; set; }
        public string[] CollectionsCriadasLista { get; set; }
    }

    /// <summary>
    /// Model para atualização de empresa
    /// </summary>
    public class AtualizarEmpresaRequest
    {
        public string? RazaoSocial { get; set; }
        public string? NomeFantasia { get; set; }
        public string? Email { get; set; }

        // Configurações WAHA
        public string? NumeroWhatsApp { get; set; }
        public string? WahaSessionName { get; set; }
        public bool? FlgWahaAtivo { get; set; }
        public bool? FlgSuspensa { get; set; }
    }

    /// <summary>
    /// Request para configurar/reconfigurar WAHA de uma empresa
    /// </summary>
    public class ConfigurarWahaRequest
    {
        [Required(ErrorMessage = "Número do WhatsApp é obrigatório")]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Número deve conter apenas dígitos (10-15 caracteres)")]
        public string NumeroWhatsApp { get; set; }

        [Required(ErrorMessage = "Nome da sessão WAHA é obrigatório")]
        public string WahaSessionName { get; set; }
    }

    /// <summary>
    /// Response da configuração WAHA
    /// </summary>
    public class ConfigurarWahaResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; }
        public ConfiguracaoWahaInfo? ConfiguracaoAnterior { get; set; }
        public ConfiguracaoWahaInfo ConfiguracaoAtual { get; set; }
        public AcoesRealizadas Acoes { get; set; }
        public List<string> Logs { get; set; } = new();
    }

    public class ConfiguracaoWahaInfo
    {
        public string? NumeroWhatsApp { get; set; }
        public string? SessionName { get; set; }
        public bool FlgAtivo { get; set; }
        public DateTime? DataConexao { get; set; }
    }

    public class AcoesRealizadas
    {
        public bool InstanciaCriada { get; set; }
        public bool InstanciaReconfigurada { get; set; }
        public bool InstanciaAtivada { get; set; }
        public bool NumeroAlterado { get; set; }
        public bool WebhookConfigurado { get; set; }
    }
}
