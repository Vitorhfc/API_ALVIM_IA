using Client_Repository.Configuration.Contexto.Interface;

namespace Client_Repository.Configuration.Contexto
{
    public class WorkerContextService : IWorkerContextService
    {
        private string? _connectionString;
        private string? _nomeBaseDados;
        private string? _usuarioId;

        public void SetContext(string connectionString, string nomeBaseDados, string usuarioId)
        {
            _connectionString = connectionString;
            _nomeBaseDados = nomeBaseDados;
            _usuarioId = usuarioId;
        }

        public string? GetConnectionString() => _connectionString;
        public string? GetNomeBaseDados() => _nomeBaseDados;
        public string? GetUsuarioId() => _usuarioId;

        public void ClearContext()
        {
            _connectionString = null;
            _nomeBaseDados = null;
            _usuarioId = null;
        }
    }
}
