using Client_Service.Service.Interface;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Classes.Entidades.ADM;

namespace Client_Service.Service
{
    /// <summary>
    /// Serviço para gerenciar o mapeamento entre Cliente e Empresa no banco Admin
    /// </summary>
    public class ClienteEmpresaMapService : IClienteEmpresaMapService
    {
        private readonly IMongoDatabase _adminDatabase;
        private readonly ILogger<ClienteEmpresaMapService> _logger;

        public ClienteEmpresaMapService(
            IMongoDatabase adminDatabase,
            ILogger<ClienteEmpresaMapService> logger)
        {
            _adminDatabase = adminDatabase;
            _logger = logger;
        }

        public async Task SincronizarMapeamentoAsync(
            string clienteId,
            string empresaId,
            string? numeroWhatsApp = null,
            string? nomeCliente = null)
        {
            try
            {
                _logger.LogInformation(
                    "Sincronizando mapeamento ClienteEmpresaMap - ClienteId: {ClienteId}, EmpresaId: {EmpresaId}",
                    clienteId,
                    empresaId
                );

                var collection = _adminDatabase.GetCollection<ClienteEmpresaMap>("ClienteEmpresaMap");

                // Buscar se já existe mapeamento
                var filter = Builders<ClienteEmpresaMap>.Filter.Eq(m => m.ClienteId, clienteId);
                var mapeamentoExistente = await collection.Find(filter).FirstOrDefaultAsync();

                if (mapeamentoExistente != null)
                {
                    // Atualizar mapeamento existente
                    var update = Builders<ClienteEmpresaMap>.Update
                        .Set(m => m.EmpresaId, empresaId)
                        .Set(m => m.DtaUltimaAtualizacao, DateTime.UtcNow)
                        .Set(m => m.FlgAtivo, true);

                    if (!string.IsNullOrWhiteSpace(numeroWhatsApp))
                        update = update.Set(m => m.NumeroWhatsApp, numeroWhatsApp);

                    if (!string.IsNullOrWhiteSpace(nomeCliente))
                        update = update.Set(m => m.NomeCliente, nomeCliente);

                    await collection.UpdateOneAsync(filter, update);

                    _logger.LogInformation(
                        "Mapeamento atualizado com sucesso - ClienteId: {ClienteId}",
                        clienteId
                    );
                }
                else
                {
                    // Criar novo mapeamento
                    var novoMapeamento = new ClienteEmpresaMap
                    {
                        ClienteId = clienteId,
                        EmpresaId = empresaId,
                        NumeroWhatsApp = numeroWhatsApp,
                        NomeCliente = nomeCliente,
                        DtaUltimaAtualizacao = DateTime.UtcNow,
                        FlgAtivo = true,
                        DtaCadastro = DateTime.UtcNow
                    };

                    await collection.InsertOneAsync(novoMapeamento);

                    _logger.LogInformation(
                        "Novo mapeamento criado com sucesso - ClienteId: {ClienteId}, MapeamentoId: {MapeamentoId}",
                        clienteId,
                        novoMapeamento.Id
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao sincronizar mapeamento - ClienteId: {ClienteId}, EmpresaId: {EmpresaId}",
                    clienteId,
                    empresaId
                );
                // Não propaga a exceção para não quebrar o fluxo principal
                // O mapeamento pode ser criado manualmente depois se necessário
            }
        }

        public async Task RemoverMapeamentoAsync(string clienteId)
        {
            try
            {
                _logger.LogInformation(
                    "Removendo mapeamento (soft delete) - ClienteId: {ClienteId}",
                    clienteId
                );

                var collection = _adminDatabase.GetCollection<ClienteEmpresaMap>("ClienteEmpresaMap");

                var filter = Builders<ClienteEmpresaMap>.Filter.Eq(m => m.ClienteId, clienteId);
                var update = Builders<ClienteEmpresaMap>.Update
                    .Set(m => m.FlgAtivo, false)
                    .Set(m => m.DtaUltimaAtualizacao, DateTime.UtcNow);

                await collection.UpdateOneAsync(filter, update);

                _logger.LogInformation(
                    "Mapeamento removido com sucesso - ClienteId: {ClienteId}",
                    clienteId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao remover mapeamento - ClienteId: {ClienteId}",
                    clienteId
                );
                // Não propaga a exceção
            }
        }

        public async Task<string?> ObterEmpresaIdPorClienteAsync(string clienteId)
        {
            try
            {
                _logger.LogInformation(
                    "Obtendo EmpresaId para ClienteId: {ClienteId}",
                    clienteId
                );

                var collection = _adminDatabase.GetCollection<ClienteEmpresaMap>("ClienteEmpresaMap");

                var filter = Builders<ClienteEmpresaMap>.Filter.And(
                    Builders<ClienteEmpresaMap>.Filter.Eq(m => m.ClienteId, clienteId),
                    Builders<ClienteEmpresaMap>.Filter.Eq(m => m.FlgAtivo, true)
                );

                var mapeamento = await collection.Find(filter).FirstOrDefaultAsync();

                if (mapeamento != null)
                {
                    _logger.LogInformation(
                        "EmpresaId encontrado - ClienteId: {ClienteId}, EmpresaId: {EmpresaId}",
                        clienteId,
                        mapeamento.EmpresaId
                    );
                    return mapeamento.EmpresaId;
                }

                _logger.LogWarning(
                    "Mapeamento não encontrado para ClienteId: {ClienteId}",
                    clienteId
                );
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao obter EmpresaId - ClienteId: {ClienteId}",
                    clienteId
                );
                return null;
            }
        }
    }
}
