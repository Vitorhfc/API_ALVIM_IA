using Client_Repository.Repositorio.Interface;
using Client_Service.DTO;
using Client_Service.Service.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service
{
    public class DashboardService : IDashboardService
    {
        private readonly IClienteRepositorio _clienteRepositorio;
        private readonly IMensagemRepositorio _mensagemRepositorio;
        private readonly IProcessamentoIARepositorio _processamentoRepositorio;

        public DashboardService(
            IClienteRepositorio clienteRepositorio,
            IMensagemRepositorio mensagemRepositorio,
            IProcessamentoIARepositorio processamentoRepositorio)
        {
            _clienteRepositorio = clienteRepositorio;
            _mensagemRepositorio = mensagemRepositorio;
            _processamentoRepositorio = processamentoRepositorio;
        }

        public async Task<DashboardData> ObterDadosDashboardAsync()
        {
            var hoje = DateTime.Now.Date;
            var mesPassado = hoje.AddMonths(-1);

            // Buscar dados em paralelo
            var (conversasAtivas, conversasAtivasMesPassado) = await ObterConversasAtivasAsync(hoje, mesPassado);
            var (mensagensHoje, mensagensMesPassado) = await ObterMensagensHojeAsync(hoje, mesPassado);
            var tempoMedio = await CalcularTempoMedioRespostaAsync();
            var satisfacao = await CalcularSatisfacaoAsync();
            var sentimentos = await ObterDadosSentimentosAsync();
            var volumeMensagens = await ObterVolumeMensagensAsync();
            var categorias = await ObterCategoriasAsync();

            return new DashboardData
            {
                Metrics = new List<MetricCard>
                {
                    new MetricCard
                    {
                        Title = "Conversas Ativas",
                        Value = conversasAtivas.ToString(),
                        Change = CalcularPercentualMudanca(conversasAtivas, conversasAtivasMesPassado),
                        ChangePositive = conversasAtivas >= conversasAtivasMesPassado,
                        Icon = "forum",
                        IconColor = "#4F85FF"
                    },
                    new MetricCard
                    {
                        Title = "Mensagens Hoje",
                        Value = FormatarNumero(mensagensHoje),
                        Change = CalcularPercentualMudanca(mensagensHoje, mensagensMesPassado),
                        ChangePositive = mensagensHoje >= mensagensMesPassado,
                        Icon = "chat_bubble",
                        IconColor = "#10B981"
                    },
                    tempoMedio,
                    satisfacao
                },
                SentimentosData = sentimentos,
                VolumeMensagensData = volumeMensagens,
                CategoriasData = categorias
            };
        }

        private async Task<(int atual, int anterior)> ObterConversasAtivasAsync(DateTime hoje, DateTime mesPassado)
        {
            var conversasAtivas = await _clienteRepositorio.BuscarContagemTotalPorFiltroAsync(
                c => c.FlgAtivo == true &&
                     c.StatusConversa == StatusConversa.Ativa);

            var conversasAtivasMesPassado = await _clienteRepositorio.BuscarContagemTotalPorFiltroAsync(
                c => c.FlgAtivo == true &&
                     c.StatusConversa == StatusConversa.Ativa &&
                     c.DtUltimaInteracao >= mesPassado &&
                     c.DtUltimaInteracao < hoje);

            return (conversasAtivas, conversasAtivasMesPassado);
        }

        private async Task<(int hoje, int mesPassado)> ObterMensagensHojeAsync(DateTime hoje, DateTime mesPassado)
        {
            var inicioDia = hoje;
            var fimDia = hoje.AddDays(1);

            var mensagensHoje = await _mensagemRepositorio.BuscarContagemTotalPorFiltroAsync(
                m => m.DtRecebido >= inicioDia && m.DtRecebido < fimDia);

            var mensagensMesPassado = await _mensagemRepositorio.BuscarContagemTotalPorFiltroAsync(
                m => m.DtRecebido >= mesPassado.AddDays(-1) && m.DtRecebido < mesPassado);

            return (mensagensHoje, mensagensMesPassado);
        }

        private async Task<MetricCard> CalcularTempoMedioRespostaAsync()
        {
            try
            {
                var hoje = DateTime.Now.Date;
                var mesPassado = hoje.AddMonths(-1);

                // Buscar mensagens dos últimos 7 dias
                var mensagens = await _mensagemRepositorio.BuscarPorFiltroAsync(
                    m => m.DtRecebido >= hoje.AddDays(-7));

                // Buscar mensagens do mês passado para comparação
                var mensagensMesPassado = await _mensagemRepositorio.BuscarPorFiltroAsync(
                    m => m.DtRecebido >= mesPassado.AddDays(-7) && m.DtRecebido < mesPassado);

                var tempoMedioAtual = CalcularTempoMedioEntreRespostas(mensagens);
                var tempoMedioAnterior = CalcularTempoMedioEntreRespostas(mensagensMesPassado);

                var mudancaPositiva = tempoMedioAtual <= tempoMedioAnterior; // Menor tempo é melhor
                var percentual = CalcularPercentualMudanca(
                    tempoMedioAnterior > 0 ? (int)tempoMedioAnterior : 0,
                    tempoMedioAtual > 0 ? (int)tempoMedioAtual : 0);

                return new MetricCard
                {
                    Title = "Tempo Médio de Resposta",
                    Value = FormatarTempo(tempoMedioAtual),
                    Change = percentual,
                    ChangePositive = mudancaPositiva,
                    Icon = "schedule",
                    IconColor = "#F59E0B"
                };
            }
            catch
            {
                return new MetricCard
                {
                    Title = "Tempo Médio de Resposta",
                    Value = "N/A",
                    Change = "0% vs mês anterior",
                    ChangePositive = true,
                    Icon = "schedule",
                    IconColor = "#F59E0B"
                };
            }
        }

        private double CalcularTempoMedioEntreRespostas(IEnumerable<Mensagem> mensagens)
        {
            var mensagensOrdenadas = mensagens.OrderBy(m => m.DtRecebido).ToList();
            if (mensagensOrdenadas.Count < 2) return 0;

            var temposResposta = new List<double>();

            for (int i = 1; i < mensagensOrdenadas.Count; i++)
            {
                var anterior = mensagensOrdenadas[i - 1];
                var atual = mensagensOrdenadas[i];

                // Verificar se é uma resposta (cliente pergunta, sistema/funcionário responde)
                if (anterior.FlgMensagemCliente && !atual.FlgMensagemCliente)
                {
                    var tempoResposta = (atual.DtRecebido - anterior.DtRecebido).TotalSeconds;
                    if (tempoResposta > 0 && tempoResposta < 3600) // Menos de 1 hora
                    {
                        temposResposta.Add(tempoResposta);
                    }
                }
            }

            return temposResposta.Any() ? temposResposta.Average() : 0;
        }

        private async Task<MetricCard> CalcularSatisfacaoAsync()
        {
            try
            {
                var hoje = DateTime.Now.Date;
                var mesPassado = hoje.AddMonths(-1);

                // Buscar processamentos com sucesso dos últimos 30 dias
                var processamentos = await _processamentoRepositorio.BuscarPorFiltroAsync(
                    p => p.DtProcessamento >= hoje.AddDays(-30) &&
                         p.Status == StatusProcessamento.Sucesso);

                var processamentosMesPassado = await _processamentoRepositorio.BuscarPorFiltroAsync(
                    p => p.DtProcessamento >= mesPassado.AddDays(-30) &&
                         p.DtProcessamento < mesPassado &&
                         p.Status == StatusProcessamento.Sucesso);

                var totalAtual = processamentos.Count();
                var sucessoAtual = processamentos.Count(p => p.Status == StatusProcessamento.Sucesso);
                var satisfacaoAtual = totalAtual > 0 ? (double)sucessoAtual / totalAtual * 5 : 0;

                var totalAnterior = processamentosMesPassado.Count();
                var sucessoAnterior = processamentosMesPassado.Count(p => p.Status == StatusProcessamento.Sucesso);
                var satisfacaoAnterior = totalAnterior > 0 ? (double)sucessoAnterior / totalAnterior * 5 : 0;

                var mudancaPositiva = satisfacaoAtual >= satisfacaoAnterior;
                var percentual = CalcularPercentualMudanca((int)(satisfacaoAnterior * 10), (int)(satisfacaoAtual * 10));

                return new MetricCard
                {
                    Title = "Taxa de Sucesso",
                    Value = $"{satisfacaoAtual:F1}/5",
                    Change = percentual,
                    ChangePositive = mudancaPositiva,
                    Icon = "star",
                    IconColor = "#8B5CF6"
                };
            }
            catch
            {
                return new MetricCard
                {
                    Title = "Taxa de Sucesso",
                    Value = "N/A",
                    Change = "0% vs mês anterior",
                    ChangePositive = true,
                    Icon = "star",
                    IconColor = "#8B5CF6"
                };
            }
        }

        private async Task<SentimentosData> ObterDadosSentimentosAsync()
        {
            try
            {
                var hoje = DateTime.Now.Date;
                var processamentos = await _processamentoRepositorio.BuscarPorFiltroAsync(
                    p => p.DtProcessamento >= hoje.AddDays(-30));

                var total = processamentos.Count();
                if (total == 0)
                {
                    return new SentimentosData { Positivo = 0, Neutro = 0, Negativo = 0 };
                }

                var sucesso = processamentos.Count(p => p.Status == StatusProcessamento.Sucesso);
                var erro = processamentos.Count(p => p.Status == StatusProcessamento.Erro);
                var timeout = total - sucesso - erro;

                return new SentimentosData
                {
                    Positivo = (int)((double)sucesso / total * 100),
                    Neutro = (int)((double)timeout / total * 100),
                    Negativo = (int)((double)erro / total * 100)
                };
            }
            catch
            {
                return new SentimentosData { Positivo = 0, Neutro = 0, Negativo = 0 };
            }
        }

        private async Task<List<VolumeMensagemData>> ObterVolumeMensagensAsync()
        {
            try
            {
                var dados = new List<VolumeMensagemData>();
                var hoje = DateTime.Now.Date;

                for (int i = 6; i >= 0; i--)
                {
                    var data = hoje.AddDays(-i);
                    var inicioDia = data;
                    var fimDia = data.AddDays(1);

                    var quantidade = await _mensagemRepositorio.BuscarContagemTotalPorFiltroAsync(
                        m => m.DtRecebido >= inicioDia && m.DtRecebido < fimDia);

                    dados.Add(new VolumeMensagemData
                    {
                        Name = data.ToString("dd/MM"),
                        Value = quantidade
                    });
                }

                return dados;
            }
            catch
            {
                return new List<VolumeMensagemData>();
            }
        }

        private async Task<List<CategoriaData>> ObterCategoriasAsync()
        {
            try
            {
                var hoje = DateTime.Now.Date;
                var processamentos = await _processamentoRepositorio.BuscarPorFiltroAsync(
                    p => p.DtProcessamento >= hoje.AddDays(-30) &&
                         !string.IsNullOrEmpty(p.TipoEsclarecimento));

                var categorias = processamentos
                    .GroupBy(p => p.TipoEsclarecimento ?? "Outros")
                    .Select(g => new CategoriaData
                    {
                        Name = g.Key,
                        Value = g.Count()
                    })
                    .OrderByDescending(c => c.Value)
                    .Take(5)
                    .ToList();

                return categorias.Any() ? categorias : new List<CategoriaData>
                {
                    new CategoriaData { Name = "Sem dados", Value = 0 }
                };
            }
            catch
            {
                return new List<CategoriaData>
                {
                    new CategoriaData { Name = "Erro ao carregar", Value = 0 }
                };
            }
        }

        private string CalcularPercentualMudanca(int valorAnterior, int valorAtual)
        {
            if (valorAnterior == 0)
            {
                return valorAtual > 0 ? "+100% vs mês anterior" : "0% vs mês anterior";
            }

            var percentual = ((double)(valorAtual - valorAnterior) / valorAnterior) * 100;
            var sinal = percentual >= 0 ? "+" : "";
            return $"{sinal}{percentual:F0}% vs mês anterior";
        }

        private string FormatarNumero(int numero)
        {
            return numero.ToString("N0").Replace(",", ".");
        }

        private string FormatarTempo(double segundos)
        {
            if (segundos < 60)
                return $"{segundos:F1}s";

            var minutos = segundos / 60;
            if (minutos < 60)
                return $"{minutos:F1}min";

            var horas = minutos / 60;
            return $"{horas:F1}h";
        }
    }
}
