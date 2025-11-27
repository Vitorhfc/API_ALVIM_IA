using Client.Controllers.Base;
using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Entidades.Client;
using System.ComponentModel;
using System.Reflection;

namespace Cliente_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TipoAgendamentoController : ControllerBaseClient<TipoAgendamentoController>
    {
        #region Construtor

        public TipoAgendamentoController(
            ILogClientService logClientService,
            ILogger<TipoAgendamentoController> logger)
            : base(logClientService, logger)
        {
        }

        #endregion

        #region Endpoints

        /// <summary>
        /// Lista todos os tipos de agendamento disponíveis
        /// </summary>
        /// <returns>Lista de tipos de agendamento</returns>
        [HttpGet]
        public async Task<IActionResult> ListarTiposAgendamento()
        {
            try
            {
                var tiposAgendamento = Enum.GetValues(typeof(TipoAgendamento))
                    .Cast<TipoAgendamento>()
                    .Select(tipo => new
                    {
                        id = (int)tipo,
                        nome = tipo.ToString(),
                        descricao = ObterDescricaoEnum(tipo)
                    })
                    .OrderBy(t => t.id)
                    .ToList();

                await LogInfoAsync($"Listou {tiposAgendamento.Count} tipos de agendamento", nameof(ListarTiposAgendamento));

                return Sucesso(tiposAgendamento, "Tipos de agendamento listados com sucesso");
            }
            catch (Exception ex)
            {
                return LogErroAsync(ex, nameof(ListarTiposAgendamento));
            }
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Obtém a descrição de um valor do enum através do atributo Description
        /// </summary>
        private string ObterDescricaoEnum(Enum valor)
        {
            var campo = valor.GetType().GetField(valor.ToString());

            if (campo == null)
                return valor.ToString();

            var atributo = campo.GetCustomAttribute<DescriptionAttribute>();

            return atributo?.Description ?? valor.ToString();
        }

        #endregion
    }
}
