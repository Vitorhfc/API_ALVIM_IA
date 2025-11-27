using Client_Service.DTO;

namespace Client_Service.Service.Interface
{
    public interface IDashboardService
    {
        Task<DashboardData> ObterDadosDashboardAsync();
    }
}
