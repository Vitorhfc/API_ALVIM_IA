using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Microsoft.Extensions.Logging;
using Shared.Classes.Entidades.Client;
using Shared.Classes.Model;

namespace Client_Service.Service
{
    public class PlanoContextoService : IPlanoContextoService
    {
        private readonly IPlanoContextoRepositorio _planoRepositorio;
        private readonly IConfiguracaoIARepositorio _configuracaoRepository;
        private readonly ILogger<PlanoContextoService> _logger;

        public PlanoContextoService(
            IPlanoContextoRepositorio planoRepositorio,
            IConfiguracaoIARepositorio configuracaoRepository,
            ILogger<PlanoContextoService> logger)
        {
            _planoRepositorio = planoRepositorio;
            _configuracaoRepository = configuracaoRepository;
            _logger = logger;
        }

        public async Task<List<PlanoContextoResponse>> BuscarTodosAsync()
        {
            var planos = await _planoRepositorio.BuscarTodosAsync();
            return planos.Select(PlanoContextoResponse.FromEntity).ToList();
        }

        public async Task<List<PlanoContextoResponse>> BuscarAtivosAsync()
        {
            var planos = await _planoRepositorio.BuscarAtivosAsync();
            return planos.Select(PlanoContextoResponse.FromEntity).ToList();
        }

        public async Task<PlanoContextoResponse?> BuscarPorIdAsync(string id)
        {
            var plano = await _planoRepositorio.BuscarPorIdAsync(id);
            return plano != null ? PlanoContextoResponse.FromEntity(plano) : null;
        }

        public async Task<PlanoContextoResponse> CriarAsync(CriarPlanoContextoRequest request)
        {
            _logger.LogInformation("Criando novo plano de contexto: {Nome}", request.Nome);

            // Verificar se já existe plano com esse tipo
            var existente = await _planoRepositorio.BuscarPorTipoAsync(request.Tipo);
            if (existente.Any())
            {
                _logger.LogWarning("Já existe plano do tipo {Tipo}", request.Tipo);
                throw new InvalidOperationException($"Já existe um plano do tipo {request.Tipo}");
            }

            var plano = new PlanoContexto
            {
                Nome = request.Nome,
                Descricao = request.Descricao,
                Tipo = request.Tipo,
                Conteudo = request.Conteudo,
                FlgAtivo = request.FlgAtivo,
                Ordem = request.Ordem,
                FlgPadrao = false // Planos criados via API não são padrão
            };

            // Se for plano base, marcar como padrão e ativo
            if (request.Tipo == TipoPlanoContexto.PlanoBase)
            {
                plano.FlgPadrao = true;
                plano.FlgAtivo = true;
            }

            var planoCriado = await _planoRepositorio.CriarAsync(plano);

            _logger.LogInformation("Plano criado com sucesso: {Id}", planoCriado.Id);

            return PlanoContextoResponse.FromEntity(planoCriado);
        }

        public async Task<PlanoContextoResponse> AtualizarAsync(string id, AtualizarPlanoContextoRequest request)
        {
            _logger.LogInformation("Atualizando plano: {Id}", id);

            var plano = await _planoRepositorio.BuscarPorIdAsync(id);
            if (plano == null)
            {
                throw new KeyNotFoundException($"Plano com ID {id} não encontrado");
            }

            // Atualizar apenas campos fornecidos
            if (!string.IsNullOrWhiteSpace(request.Nome))
            {
                plano.Nome = request.Nome;
            }

            if (request.Descricao != null)
            {
                plano.Descricao = request.Descricao;
            }

            if (!string.IsNullOrWhiteSpace(request.Conteudo))
            {
                plano.Conteudo = request.Conteudo;
            }

            if (request.FlgAtivo.HasValue)
            {
                // Plano base não pode ser desativado
                if (plano.Tipo == TipoPlanoContexto.PlanoBase)
                {
                    plano.FlgAtivo = true;
                }
                else
                {
                    plano.FlgAtivo = request.FlgAtivo.Value;
                }
            }

            if (request.Ordem.HasValue)
            {
                plano.Ordem = request.Ordem.Value;
            }

            var planoAtualizado = await _planoRepositorio.AtualizarAsync(plano);

            _logger.LogInformation("Plano atualizado com sucesso: {Id}", id);

            return PlanoContextoResponse.FromEntity(planoAtualizado);
        }

        public async Task<bool> DeletarAsync(string id)
        {
            _logger.LogInformation("Deletando plano: {Id}", id);

            var plano = await _planoRepositorio.BuscarPorIdAsync(id);
            if (plano == null)
            {
                throw new KeyNotFoundException($"Plano com ID {id} não encontrado");
            }

            if (plano.FlgPadrao)
            {
                throw new InvalidOperationException("Não é possível deletar planos padrão do sistema");
            }

            var deletado = await _planoRepositorio.DeletarAsync(id);

            if (deletado)
            {
                _logger.LogInformation("Plano deletado com sucesso: {Id}", id);
            }

            return deletado;
        }

        public async Task<ConfigurarPlanosResponse> ConfigurarPlanosAsync(ConfigurarPlanosRequest request)
        {
            _logger.LogInformation("Configurando {Count} planos", request.Planos.Count);

            var response = new ConfigurarPlanosResponse
            {
                Sucesso = true,
                Planos = new List<PlanoAtualizadoInfo>()
            };

            foreach (var item in request.Planos)
            {
                var plano = await _planoRepositorio.BuscarPorIdAsync(item.PlanoId);

                if (plano == null)
                {
                    _logger.LogWarning("Plano não encontrado: {PlanoId}", item.PlanoId);
                    response.Avisos.Add($"Plano {item.PlanoId} não encontrado");
                    continue;
                }

                var flgAtivoAnterior = plano.FlgAtivo;
                var flgAtivoNovo = item.FlgAtivo;

                // REGRA: Plano Base sempre ativo
                if (plano.Tipo == TipoPlanoContexto.PlanoBase && !item.FlgAtivo)
                {
                    _logger.LogWarning("Tentativa de desativar Plano Base foi bloqueada");
                    flgAtivoNovo = true; // Forçar ativo
                    response.Avisos.Add("Plano Base não pode ser desativado - mantido como ativo");
                }

                var atualizado = await _planoRepositorio.AlterarStatusAsync(item.PlanoId, flgAtivoNovo);

                response.Planos.Add(new PlanoAtualizadoInfo
                {
                    PlanoId = plano.Id,
                    Nome = plano.Nome,
                    Tipo = plano.Tipo,
                    FlgAtivoAnterior = flgAtivoAnterior,
                    FlgAtivoNovo = flgAtivoNovo,
                    Atualizado = atualizado,
                    MotivoNaoAtualizado = atualizado ? null : "Falha ao atualizar no banco"
                });

                if (atualizado)
                {
                    response.PlanosAtualizados++;
                }
            }

            response.Mensagem = $"{response.PlanosAtualizados} plano(s) atualizado(s) com sucesso";

            _logger.LogInformation(
                "Configuração de planos concluída: {Atualizados}/{Total}",
                response.PlanosAtualizados,
                request.Planos.Count
            );

            return response;
        }

        public async Task<bool> AplicarPlanosNaConfiguracaoAsync(string configuracaoIAId)
        {
            _logger.LogInformation("Aplicando planos na ConfiguracaoIA: {Id}", configuracaoIAId);

            var configuracao = await _configuracaoRepository.BuscarPorIdAsync(configuracaoIAId);
            if (configuracao == null)
            {
                _logger.LogWarning("ConfiguracaoIA não encontrada: {Id}", configuracaoIAId);
                return false;
            }

            var planosAtivos = await _planoRepositorio.BuscarAtivosAsync();

            _logger.LogInformation("Aplicando {Count} planos ativos", planosAtivos.Count);

            foreach (var plano in planosAtivos)
            {
                switch (plano.Tipo)
                {
                    case TipoPlanoContexto.PlanoBase:
                        configuracao.InformacoesGerais = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.FuncaoPrincipal:
                        configuracao.FuncaoPrincipalDoProduto = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.ModulosFuncionalidades:
                        configuracao.ModulosFuncionalidadesDoProduto = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.ProcessoDeUso:
                        configuracao.ProcessoDeUsoProduto = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.FAQ:
                        configuracao.PerguntasFrequentesSobreProduto = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.DoresAtendidas:
                        configuracao.DoresAtendidasPeloProduto = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.DiferenciaisVantagens:
                        configuracao.DiferencasVantagensDoProduto = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.IntegracoesRecursos:
                        configuracao.IntegracoesRecursosExtrasProduto = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.SuporteAtendimento:
                        configuracao.SuporteEAtendimentoDoProduto = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.PlanosPrecos:
                        configuracao.PlanosPrecosCondicoesComerciaisDoProduto = plano.Conteudo;
                        break;
                    case TipoPlanoContexto.CasosDeUso:
                        configuracao.CasosDeUsoExemplosPraticosEValoresSistema = plano.Conteudo;
                        break;
                }
            }

            configuracao.DtaAlteracao = DateTime.UtcNow;
            await _configuracaoRepository.EditarAsync(configuracao);

            _logger.LogInformation("Planos aplicados com sucesso na ConfiguracaoIA: {Id}", configuracaoIAId);

            return true;
        }

        public async Task InicializarPlanosPadraoAsync()
        {
            _logger.LogInformation("Verificando se planos padrão precisam ser criados");

            // Verificar se Plano Base já existe
            var planoBase = await _planoRepositorio.BuscarPlanoBaseAsync();
            if (planoBase != null)
            {
                _logger.LogInformation("Planos padrão já existem");
                return;
            }

            _logger.LogInformation("Criando planos padrão do sistema");

            var planosPadrao = new List<PlanoContexto>
            {
                new PlanoContexto
                {
                    Nome = "Plano Base",
                    Descricao = "Informações gerais do produto/serviço - SEMPRE ATIVO",
                    Tipo = TipoPlanoContexto.PlanoBase,
                    Conteudo = "Configure aqui as informações gerais do seu produto ou serviço.",
                    FlgAtivo = true,
                    FlgPadrao = true,
                    Ordem = 1
                }
            };

            foreach (var plano in planosPadrao)
            {
                await _planoRepositorio.CriarAsync(plano);
            }

            _logger.LogInformation("Planos padrão criados com sucesso");
        }
    }
}
