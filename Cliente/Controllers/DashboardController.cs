using Client_Service.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Cliente_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var dashboardData = await _dashboardService.ObterDadosDashboardAsync();
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao obter dados da dashboard", error = ex.Message });
            }
        }
    }
}