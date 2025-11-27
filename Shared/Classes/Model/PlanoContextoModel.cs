using Shared.Classes.Entidades.Client;
using System.ComponentModel.DataAnnotations;

namespace Shared.Classes.Model
{
    #region Request Models

    /// <summary>
    /// Request para criar um novo plano de contexto
    /// </summary>
    public class CriarPlanoContextoRequest
    {
        [Required(ErrorMessage = "Nome do plano é obrigatório")]
        [StringLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        [Required(ErrorMessage = "Tipo do plano é obrigatório")]
        public TipoPlanoContexto Tipo { get; set; }

        [Required(ErrorMessage = "Conteúdo do plano é obrigatório")]
        [MinLength(10, ErrorMessage = "Conteúdo deve ter no mínimo 10 caracteres")]
        public string Conteudo { get; set; } = string.Empty;

        public bool FlgAtivo { get; set; } = true;

        public int Ordem { get; set; } = 999;
    }

    /// <summary>
    /// Request para atualizar um plano de contexto
    /// </summary>
    public class AtualizarPlanoContextoRequest
    {
        public string? Nome { get; set; }

        public string? Descricao { get; set; }

        public string? Conteudo { get; set; }

        public bool? FlgAtivo { get; set; }

        public int? Ordem { get; set; }
    }

    /// <summary>
    /// Request para configurar múltiplos planos (ativar/desativar)
    /// </summary>
    public class ConfigurarPlanosRequest
    {
        [Required(ErrorMessage = "Lista de configurações é obrigatória")]
        [MinLength(1, ErrorMessage = "Deve haver pelo menos um plano na lista")]
        public List<PlanoConfiguracaoItem> Planos { get; set; } = new();
    }

    /// <summary>
    /// Item de configuração de plano
    /// </summary>
    public class PlanoConfiguracaoItem
    {
        [Required(ErrorMessage = "ID do plano é obrigatório")]
        public string PlanoId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status ativo/inativo é obrigatório")]
        public bool FlgAtivo { get; set; }
    }

    #endregion

    #region Response Models

    /// <summary>
    /// Response com dados de um plano de contexto
    /// </summary>
    public class PlanoContextoResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public TipoPlanoContexto Tipo { get; set; }
        public string TipoDescricao { get; set; } = string.Empty;
        public bool FlgAtivo { get; set; }
        public string Conteudo { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public bool FlgPadrao { get; set; }
        public bool PodeDesativar { get; set; } // False se for PlanoBase
        public bool PodeDeletar { get; set; } // False se FlgPadrao = true
        public DateTime DtCriacao { get; set; }
        public DateTime DtAlteracao { get; set; }

        public static PlanoContextoResponse FromEntity(PlanoContexto plano)
        {
            return new PlanoContextoResponse
            {
                Id = plano.Id,
                Nome = plano.Nome,
                Descricao = plano.Descricao,
                Tipo = plano.Tipo,
                TipoDescricao = ObterDescricaoTipo(plano.Tipo),
                FlgAtivo = plano.FlgAtivo,
                Conteudo = plano.Conteudo,
                Ordem = plano.Ordem,
                FlgPadrao = plano.FlgPadrao,
                PodeDesativar = plano.Tipo != TipoPlanoContexto.PlanoBase,
                PodeDeletar = !plano.FlgPadrao,
                DtCriacao = plano.DtCriacao,
                DtAlteracao = plano.DtAlteracao
            };
        }

        private static string ObterDescricaoTipo(TipoPlanoContexto tipo)
        {
            return tipo switch
            {
                TipoPlanoContexto.PlanoBase => "Plano Base (Sempre Ativo)",
                TipoPlanoContexto.FuncaoPrincipal => "Função Principal do Produto",
                TipoPlanoContexto.ModulosFuncionalidades => "Módulos e Funcionalidades",
                TipoPlanoContexto.ProcessoDeUso => "Processo de Uso",
                TipoPlanoContexto.FAQ => "Perguntas Frequentes (FAQ)",
                TipoPlanoContexto.DoresAtendidas => "Dores Atendidas",
                TipoPlanoContexto.DiferenciaisVantagens => "Diferenciais e Vantagens",
                TipoPlanoContexto.IntegracoesRecursos => "Integrações e Recursos Extras",
                TipoPlanoContexto.SuporteAtendimento => "Suporte e Atendimento",
                TipoPlanoContexto.PlanosPrecos => "Planos, Preços e Condições",
                TipoPlanoContexto.CasosDeUso => "Casos de Uso e Exemplos",
                TipoPlanoContexto.InstrucoesDocumentos => "Instruções para Documentos",
                TipoPlanoContexto.InstrucoesAgendamento => "Instruções para Agendamento",
                TipoPlanoContexto.InstrucoesArquivos => "Instruções para Arquivos",
                _ => "Tipo Desconhecido"
            };
        }
    }

    /// <summary>
    /// Response da configuração de múltiplos planos
    /// </summary>
    public class ConfigurarPlanosResponse
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public int PlanosAtualizados { get; set; }
        public List<PlanoAtualizadoInfo> Planos { get; set; } = new();
        public List<string> Avisos { get; set; } = new();
        public DateTime DtProcessamento { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Informação de plano atualizado
    /// </summary>
    public class PlanoAtualizadoInfo
    {
        public string PlanoId { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public TipoPlanoContexto Tipo { get; set; }
        public bool FlgAtivoAnterior { get; set; }
        public bool FlgAtivoNovo { get; set; }
        public bool Atualizado { get; set; }
        public string? MotivoNaoAtualizado { get; set; }
    }

    #endregion
}
