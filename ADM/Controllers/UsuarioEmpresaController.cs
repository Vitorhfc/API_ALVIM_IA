using ADM.Controllers.Base;
using Admin_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.Entidades.ADM;

namespace ADM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuarioEmpresaController : ControllerBaseComplemento<UsuarioEmpresaController>
    {
        private readonly IUsuarioEmpresaService _usuarioEmpresaService;

        public UsuarioEmpresaController(IUsuarioEmpresaService usuarioEmpresaService, ILogADMService logADMService, ILogger<UsuarioEmpresaController> logger)
            : base(logADMService, logger)
        {
            _usuarioEmpresaService = usuarioEmpresaService;
        }

        [HttpGet]
        public async Task<IActionResult> ListarVinculos()
        {
            try
            {
                var vinculos = await _usuarioEmpresaService.BuscarTodosAsync();
                await LogInfoAsync($"Listou {vinculos.Count()} vínculos", nameof(ListarVinculos));
                return Sucesso(vinculos, "Vínculos listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarVinculos));
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarVinculoPorId(string id)
        {
            try
            {
                var vinculo = await _usuarioEmpresaService.BuscarPorIdAsync(id);

                if (vinculo == null)
                {
                    await LogInfoAsync($"Tentativa de buscar vínculo inexistente: {id}", nameof(BuscarVinculoPorId));
                    return Erro("Vínculo não encontrado");
                }

                await LogInfoAsync($"Buscou vínculo: {id}", nameof(BuscarVinculoPorId));
                return Sucesso(vinculo, "Vínculo encontrado");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarVinculoPorId), $"ID: {id}");
            }
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> BuscarVinculosPorUsuario(string usuarioId)
        {
            try
            {
                var vinculos = await _usuarioEmpresaService.BuscarPorUsuarioIdAsync(usuarioId);
                await LogInfoAsync($"Buscou vínculos do usuário: {usuarioId}", nameof(BuscarVinculosPorUsuario));
                return Sucesso(vinculos, "Vínculos encontrados");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarVinculosPorUsuario), $"UsuarioId: {usuarioId}");
            }
        }

        [HttpGet("empresa/{empresaId}")]
        public async Task<IActionResult> BuscarVinculosPorEmpresa(string empresaId)
        {
            try
            {
                var vinculos = await _usuarioEmpresaService.BuscarPorEmpresaIdAsync(empresaId);
                await LogInfoAsync($"Buscou vínculos da empresa: {empresaId}", nameof(BuscarVinculosPorEmpresa));
                return Sucesso(vinculos, "Vínculos encontrados");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarVinculosPorEmpresa), $"EmpresaId: {empresaId}");
            }
        }

        [HttpGet("usuario/{usuarioId}/empresa/{empresaId}")]
        public async Task<IActionResult> BuscarVinculoEspecifico(string usuarioId, string empresaId)
        {
            try
            {
                var vinculo = await _usuarioEmpresaService.BuscarPorUsuarioEmpresaAsync(usuarioId, empresaId);

                if (vinculo == null)
                {
                    await LogInfoAsync($"Vínculo não encontrado: UsuarioId {usuarioId}, EmpresaId {empresaId}", nameof(BuscarVinculoEspecifico));
                    return Erro("Vínculo não encontrado");
                }

                await LogInfoAsync($"Buscou vínculo específico: UsuarioId {usuarioId}, EmpresaId {empresaId}", nameof(BuscarVinculoEspecifico));
                return Sucesso(vinculo, "Vínculo encontrado");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarVinculoEspecifico), $"UsuarioId: {usuarioId}, EmpresaId: {empresaId}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CriarVinculo([FromBody] UsuarioEmpresa usuarioEmpresa)
        {
            try
            {
                if (usuarioEmpresa == null)
                    return Erro("Dados do vínculo são obrigatórios");

                var vinculoCriado = await _usuarioEmpresaService.AdicionarAsync(usuarioEmpresa);

                await RegistraAcaoAsync(
                    "Criar Vínculo",
                    "Novo vínculo",
                    SerializarParaLog(vinculoCriado),
                    $"Vínculo criado com sucesso");

                return Sucesso(vinculoCriado, "Vínculo criado com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(CriarVinculo), SerializarParaLog(usuarioEmpresa));
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarVinculo(string id, [FromBody] UsuarioEmpresa usuarioEmpresa)
        {
            try
            {
                if (usuarioEmpresa == null)
                    return Erro("Dados do vínculo são obrigatórios");

                usuarioEmpresa.Id = id;

                var vinculoAntigo = await _usuarioEmpresaService.BuscarPorIdAsync(id);
                if (vinculoAntigo == null)
                    return Erro("Vínculo não encontrado");

                var vinculoAtualizado = await _usuarioEmpresaService.EditarAsync(usuarioEmpresa);

                await RegistraAcaoAsync(
                    "Atualizar Vínculo",
                    SerializarParaLog(vinculoAntigo),
                    SerializarParaLog(vinculoAtualizado),
                    $"Vínculo atualizado com sucesso");

                return Sucesso(vinculoAtualizado, "Vínculo atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(AtualizarVinculo), $"ID: {id}, Dados: {SerializarParaLog(usuarioEmpresa)}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoverVinculo(string id)
        {
            try
            {
                var vinculo = await _usuarioEmpresaService.BuscarPorIdAsync(id);
                if (vinculo == null)
                    return Erro("Vínculo não encontrado");

                await _usuarioEmpresaService.ExcluirPorIdAsync(id);

                await RegistraAcaoAsync(
                    "Remover Vínculo",
                    SerializarParaLog(vinculo),
                    "Vínculo removido",
                    $"Vínculo removido com sucesso");

                return Sucesso(null, "Vínculo removido com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(RemoverVinculo), $"ID: {id}");
            }
        }
    }
}