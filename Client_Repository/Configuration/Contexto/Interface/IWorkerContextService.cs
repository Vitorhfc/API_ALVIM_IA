using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client_Repository.Configuration.Contexto.Interface
{
    public interface IWorkerContextService
    {
        void SetContext(string connectionString, string nomeBaseDados, string usuarioId);
        string? GetConnectionString();
        string? GetNomeBaseDados();
        string? GetUsuarioId();
        void ClearContext();
    }
}
