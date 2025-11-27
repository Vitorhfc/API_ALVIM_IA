using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Repositorio.Interface;
using Client_Repository.RepositorioGenerico;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using Shared.Classes.Entidades.Client;
using System.Threading.Tasks;

namespace Client_Repository.Repositorio
{
    public class ClienteRepositorio : RepositorioGenerico<Cliente>, IClienteRepositorio
    {
        public ClienteRepositorio(
            IContextoMultiTenantService contextoMultiTenant,
            IHttpContextAccessor httpContextAccessor)
            : base(contextoMultiTenant, httpContextAccessor, "Cliente")
        {
        }

        public async Task<Cliente?> BuscarPorNumeroAsync(string numero)
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(c => c.Numero == numero).FirstOrDefaultAsync();
        }

        public async Task<Cliente?> BuscarPorNumeroInternoAsync(string numeroInterno)
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(c => c.NumeroInterno == numeroInterno).FirstOrDefaultAsync();
        }

        public async Task<Cliente?> BuscarPorNumeroWahaAsync(string numeroTelefoneWaha)
        {
            var collection = await ObterColecaoAsync();
            return await collection.Find(c => c.NumeroTelefoneWaha == numeroTelefoneWaha).FirstOrDefaultAsync();
        }

        public async Task AtualizarUltimaInteracaoAsync(string clienteId, DateTime dataInteracao)
        {
            var collection = await ObterColecaoAsync();
            var update = Builders<Cliente>.Update.Set(c => c.DtUltimaInteracao, dataInteracao);
            await collection.UpdateOneAsync(c => c.Id == clienteId, update);
        }

        public async Task AtualizarContextoAsync(string clienteId, ContextoAtual contexto)
        {
            var collection = await ObterColecaoAsync();
            var update = Builders<Cliente>.Update.Set(c => c.Contexto, contexto);
            await collection.UpdateOneAsync(c => c.Id == clienteId, update);
        }

        public async Task AtualizarStatusConversaAsync(string clienteId, StatusConversa status)
        {
            var collection = await ObterColecaoAsync();
            var updates = new List<UpdateDefinition<Cliente>>
            {
                Builders<Cliente>.Update.Set(c => c.StatusConversa, status)
            };

            if (status == StatusConversa.Finalizada)
            {
                updates.Add(Builders<Cliente>.Update.Set(c => c.DtFinalizacaoConversa, DateTime.UtcNow));
            }

            var combinedUpdate = Builders<Cliente>.Update.Combine(updates);
            await collection.UpdateOneAsync(c => c.Id == clienteId, combinedUpdate);
        }

        public async Task InserirAsync(Cliente cliente)
        {
            var collection = await ObterColecaoAsync();
            cliente.DtPrimeiroContato = DateTime.UtcNow;
            cliente.DtUltimaInteracao = DateTime.UtcNow;
            await collection.InsertOneAsync(cliente);
        }

        public async Task AtualizarAsync(Cliente cliente)
        {
            var collection = await ObterColecaoAsync();
            cliente.DtaAlteracao = DateTime.UtcNow;
            await collection.ReplaceOneAsync(c => c.Id == cliente.Id, cliente);
        }

        /// <summary>
        /// Busca ou cria cliente usando FindOneAndUpdate (operação atômica)
        /// Evita duplicação mesmo com condições de corrida
        /// </summary>
        public async Task<Cliente> BuscarOuCriarClienteAtomicoAsync(Cliente novoCliente)
        {
            var collection = await ObterColecaoAsync();

            // Filtro: busca por numeroTelefoneWaha
            var filter = Builders<Cliente>.Filter.Eq(c => c.NumeroTelefoneWaha, novoCliente.NumeroTelefoneWaha);

            // Update: define os campos se estiver criando OU atualiza informações se já existe
            var updateBuilder = Builders<Cliente>.Update;
            var updates = new List<UpdateDefinition<Cliente>>
            {
                // ✅ Campos que só são definidos na criação
                updateBuilder.SetOnInsert(c => c.Numero, novoCliente.Numero),
                updateBuilder.SetOnInsert(c => c.NumeroTelefoneWaha, novoCliente.NumeroTelefoneWaha),
                updateBuilder.SetOnInsert(c => c.NumeroInterno, novoCliente.NumeroInterno),
                updateBuilder.SetOnInsert(c => c.Email, novoCliente.Email ?? ""),
                updateBuilder.SetOnInsert(c => c.Cpf, novoCliente.Cpf ?? ""),
                updateBuilder.SetOnInsert(c => c.StatusConversa, novoCliente.StatusConversa),
                updateBuilder.SetOnInsert(c => c.DtPrimeiroContato, DateTime.UtcNow),
                updateBuilder.SetOnInsert(c => c.DtaCadastro, DateTime.UtcNow),
                updateBuilder.SetOnInsert(c => c.FlgAtivo, true),
                updateBuilder.SetOnInsert(c => c.TotalMensagens, 0),
                updateBuilder.SetOnInsert(c => c.Contexto, novoCliente.Contexto ?? new ContextoAtual
                {
                    DtUltimaAtualizacao = DateTime.UtcNow,
                    EntidadesExtraidas = new Dictionary<string, string>()
                }),
                updateBuilder.SetOnInsert(c => c.FlgRespostaResponsavel, false),

                // ✅ Sempre atualiza a última interação e data de alteração
                updateBuilder.Set(c => c.DtUltimaInteracao, DateTime.UtcNow),
                updateBuilder.Set(c => c.DtaAlteracao, DateTime.UtcNow)
            };

            // ✅ Nome: Atualiza sempre se temos um nome melhor (não "Desconhecido" ou vazio)
            if (!string.IsNullOrWhiteSpace(novoCliente.Nome) && novoCliente.Nome != "Desconhecido")
            {
                updates.Add(updateBuilder.Set(c => c.Nome, novoCliente.Nome));
            }
            else
            {
                updates.Add(updateBuilder.SetOnInsert(c => c.Nome, novoCliente.Nome));
            }

            // ✅ Foto: Atualiza sempre se temos uma foto disponível
            if (!string.IsNullOrWhiteSpace(novoCliente.FotoPerfilUrl))
            {
                updates.Add(updateBuilder.Set(c => c.FotoPerfilUrl, novoCliente.FotoPerfilUrl));
            }
            else
            {
                updates.Add(updateBuilder.SetOnInsert(c => c.FotoPerfilUrl, novoCliente.FotoPerfilUrl));
            }

            // ✅ Informações do WhatsApp: Atualiza sempre quando disponíveis (vieram da API WAHA)
            if (!string.IsNullOrWhiteSpace(novoCliente.PushName))
            {
                updates.Add(updateBuilder.Set(c => c.PushName, novoCliente.PushName));
            }
            else
            {
                updates.Add(updateBuilder.SetOnInsert(c => c.PushName, novoCliente.PushName));
            }

            if (!string.IsNullOrWhiteSpace(novoCliente.WhatsAppId))
            {
                updates.Add(updateBuilder.Set(c => c.WhatsAppId, novoCliente.WhatsAppId));
            }
            else
            {
                updates.Add(updateBuilder.SetOnInsert(c => c.WhatsAppId, novoCliente.WhatsAppId));
            }

            if (novoCliente.IsMyContact.HasValue)
            {
                updates.Add(updateBuilder.Set(c => c.IsMyContact, novoCliente.IsMyContact));
            }
            else
            {
                updates.Add(updateBuilder.SetOnInsert(c => c.IsMyContact, novoCliente.IsMyContact));
            }

            if (novoCliente.IsWAContact.HasValue)
            {
                updates.Add(updateBuilder.Set(c => c.IsWAContact, novoCliente.IsWAContact));
            }
            else
            {
                updates.Add(updateBuilder.SetOnInsert(c => c.IsWAContact, novoCliente.IsWAContact));
            }

            if (novoCliente.DtUltimaAtualizacaoWaha.HasValue)
            {
                updates.Add(updateBuilder.Set(c => c.DtUltimaAtualizacaoWaha, novoCliente.DtUltimaAtualizacaoWaha));
            }
            else
            {
                updates.Add(updateBuilder.SetOnInsert(c => c.DtUltimaAtualizacaoWaha, novoCliente.DtUltimaAtualizacaoWaha));
            }

            var update = updateBuilder.Combine(updates);

            // Opções: upsert = true (cria se não existir), ReturnDocument.After (retorna o documento atualizado)
            var options = new FindOneAndUpdateOptions<Cliente>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            // Executa operação atômica
            var resultado = await collection.FindOneAndUpdateAsync(filter, update, options);

            return resultado;
        }

        /// <summary>
        /// Cria índice único para NumeroTelefoneWaha
        /// Previne duplicatas no nível do banco de dados
        /// </summary>
        public async Task CriarIndiceUnicoNumeroWahaAsync()
        {
            try
            {
                var collection = await ObterColecaoAsync();

                // Define o índice único
                var indexKeys = Builders<Cliente>.IndexKeys.Ascending(c => c.NumeroTelefoneWaha);
                var indexOptions = new CreateIndexOptions
                {
                    Unique = true,
                    Name = "idx_numeroTelefoneWaha_unique",
                    Background = true // Cria em background para não bloquear
                };

                var indexModel = new CreateIndexModel<Cliente>(indexKeys, indexOptions);

                // Verifica se o índice já existe
                var existingIndexes = await collection.Indexes.ListAsync();
                var indexList = await existingIndexes.ToListAsync();

                var indexExists = indexList.Any(idx =>
                    idx.Contains("name") && idx["name"].AsString == "idx_numeroTelefoneWaha_unique");

                if (!indexExists)
                {
                    await collection.Indexes.CreateOneAsync(indexModel);
                }
            }
            catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict")
            {
                // Índice já existe com configuração diferente - pode ignorar
            }
            catch (Exception)
            {
                // Em caso de erro, não bloqueia a aplicação
                // O método atômico ainda funcionará, mas sem a proteção extra do índice
            }
        }
    }
}