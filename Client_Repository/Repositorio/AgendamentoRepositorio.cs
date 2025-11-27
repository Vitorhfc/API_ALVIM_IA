using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio
{
    public class AgendamentoRepositorio : RepositorioGenerico<Agendamento>, IAgendamentoRepositorio
    {
        public AgendamentoRepositorio(
            IContextoMultiTenantService contextoMultiTenant,
            IHttpContextAccessor httpContextAccessor)
            : base(contextoMultiTenant, httpContextAccessor, "Agendamento")
        {
        }

        public async Task<IEnumerable<Agendamento>> BuscarAgendamentosPorMesAsync(int ano, int mes)
        {
            var primeiroDiaMes = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Local);
            var ultimoDiaMes = primeiroDiaMes.AddMonths(1).AddDays(-1).Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            var collection = await ObterColecaoAsync();
            return await collection
                .Find(a => a.DataHoraInicio >= primeiroDiaMes && a.DataHoraInicio <= ultimoDiaMes)
                .SortBy(a => a.DataHoraInicio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Agendamento>> BuscarAgendamentosPorDiaAsync(DateTime data)
        {
            var inicioDia = data.Date;
            var fimDia = data.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            var collection = await ObterColecaoAsync();
            return await collection
                .Find(a => a.DataHoraInicio >= inicioDia && a.DataHoraInicio <= fimDia)
                .SortBy(a => a.DataHoraInicio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Agendamento>> BuscarAgendamentosPorFuncionarioAsync(string funcionarioId, DateTime dataInicio, DateTime dataFim)
        {
            var inicioFiltro = dataInicio.Date;
            var fimFiltro = dataFim.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            var collection = await ObterColecaoAsync();
            return await collection
                .Find(a => a.FuncionarioIds.Contains(funcionarioId) &&
                           a.DataHoraInicio >= inicioFiltro &&
                           a.DataHoraInicio <= fimFiltro)
                .SortBy(a => a.DataHoraInicio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Agendamento>> BuscarAgendamentosPorClienteAsync(string clienteId)
        {
            var collection = await ObterColecaoAsync();
            return await collection
                .Find(a => a.ClienteId == clienteId)
                .SortByDescending(a => a.DataHoraInicio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Agendamento>> BuscarAgendamentosPorPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            var inicioFiltro = dataInicio.Date;
            var fimFiltro = dataFim.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            var collection = await ObterColecaoAsync();
            return await collection
                .Find(a => a.DataHoraInicio >= inicioFiltro && a.DataHoraInicio <= fimFiltro)
                .SortBy(a => a.DataHoraInicio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Agendamento>> BuscarAgendamentosPorStatusAsync(StatusAgendamento status)
        {
            var collection = await ObterColecaoAsync();
            return await collection
                .Find(a => a.Status == status)
                .SortBy(a => a.DataHoraInicio)
                .ToListAsync();
        }

        public async Task<bool> VerificarConflitoHorarioAsync(string funcionarioId, DateTime dataHoraInicio, DateTime dataHoraFim, string? agendamentoIdExcluir = null)
        {
            var collection = await ObterColecaoAsync();

            var filter = Builders<Agendamento>.Filter.And(
                Builders<Agendamento>.Filter.AnyEq(a => a.FuncionarioIds, funcionarioId),
                Builders<Agendamento>.Filter.Or(
                    // Novo agendamento começa durante um agendamento existente
                    Builders<Agendamento>.Filter.And(
                        Builders<Agendamento>.Filter.Lte(a => a.DataHoraInicio, dataHoraInicio),
                        Builders<Agendamento>.Filter.Gt(a => a.DataHoraFim, dataHoraInicio)
                    ),
                    // Novo agendamento termina durante um agendamento existente
                    Builders<Agendamento>.Filter.And(
                        Builders<Agendamento>.Filter.Lt(a => a.DataHoraInicio, dataHoraFim),
                        Builders<Agendamento>.Filter.Gte(a => a.DataHoraFim, dataHoraFim)
                    ),
                    // Novo agendamento engloba completamente um agendamento existente
                    Builders<Agendamento>.Filter.And(
                        Builders<Agendamento>.Filter.Gte(a => a.DataHoraInicio, dataHoraInicio),
                        Builders<Agendamento>.Filter.Lte(a => a.DataHoraFim, dataHoraFim)
                    )
                ),
                // Excluir agendamentos cancelados da verificação
                Builders<Agendamento>.Filter.Ne(a => a.Status, StatusAgendamento.Cancelado)
            );

            // Se estiver editando, excluir o próprio agendamento da verificação
            if (!string.IsNullOrWhiteSpace(agendamentoIdExcluir))
            {
                filter = Builders<Agendamento>.Filter.And(
                    filter,
                    Builders<Agendamento>.Filter.Ne(a => a.Id, agendamentoIdExcluir)
                );
            }

            var conflitos = await collection.CountDocumentsAsync(filter);
            return conflitos > 0;
        }
    }
}
