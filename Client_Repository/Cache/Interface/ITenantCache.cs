using Shared.Classes.Model;

namespace Client_Repository.Cache.Interface
{
    public interface ITenantCache
    {
        void SetTenant(string usuarioId, string empresaId, string connectionString, string nomeBaseDados);
        TenantInfo? GetTenant(string usuarioId, string empresaId);
        bool HasTenant(string usuarioId, string empresaId);
        void RemoveTenant(string usuarioId, string empresaId);
        void RemoveAllUserTenants(string usuarioId);
    }
}
