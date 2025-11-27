using ADM.Controllers.Base;
using Admin_Service.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Classes.ModelView.ADM;
using Shared.Utils.Criptografia;

namespace ADM.Controllers
{
    /// <summary>
    /// Controlador para gerenciamento de usuários
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuarioController : ControllerBaseComplemento<UsuarioController>
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService, ILogADMService logADMService, ILogger<UsuarioController> logger)
            : base(logADMService, logger)
        {
            _usuarioService = usuarioService;
        }

        /// <summary>
        /// Lista todos os usuários
        /// </summary>
        /// <returns>Lista de usuários</returns>
        [HttpGet]
        public async Task<IActionResult> ListarUsuarios()
        {
            try
            {
                var usuarios = await _usuarioService.BuscarTodosAsync();

                await LogInfoAsync($"Listou {usuarios.Count()} usuários", nameof(ListarUsuarios));

                return Sucesso(usuarios, "Usuários listados com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(ListarUsuarios));
            }
        }

        /// <summary>
        /// Busca usuário por ID
        /// </summary>
        /// <param name="id">ID do usuário</param>
        /// <returns>Usuário encontrado</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarUsuarioPorId(string id)
        {
            try
            {
                var usuario = await _usuarioService.BuscarPorIdAsync(id);

                if (usuario == null)
                {
                    await LogInfoAsync($"Tentativa de buscar usuário inexistente: {id}", nameof(BuscarUsuarioPorId));
                    return Erro("Usuário não encontrado");
                }

                await LogInfoAsync($"Buscou usuário: {usuario.Nome}", nameof(BuscarUsuarioPorId));

                return Sucesso(usuario, "Usuário encontrado");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(BuscarUsuarioPorId), $"ID: {id}");
            }
        }

        /// <summary>
        /// Cadastra um novo usuário
        /// </summary>
        /// <param name="model">Dados do usuário</param>
        /// <returns>Usuário cadastrado</returns>
        [AllowAnonymous]
        [HttpPost("cadastrar")]
        public async Task<IActionResult> CadastrarUsuario([FromBody] UsuarioModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Erro("Dados do usuário são inválidos");
                }

                var dadosAntigos = "Novo usuário";
                var usuarioCriado = await _usuarioService.CadastrarUsuarioAsync(model);

                await RegistraAcaoAsync(
                    "Cadastrar Usuário",
                    dadosAntigos,
                    SerializarParaLog(usuarioCriado),
                    $"Usuário {model.Nome} cadastrado com sucesso");

                return Sucesso(usuarioCriado, "Usuário cadastrado com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(CadastrarUsuario), SerializarParaLog(model));
            }
        }

        /// <summary>
        /// Atualiza um usuário existente
        /// </summary>
        /// <param name="id">ID do usuário</param>
        /// <param name="model">Dados atualizados do usuário</param>
        /// <returns>Usuário atualizado</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarUsuario(string id, [FromBody] UsuarioModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Erro("Dados do usuário são inválidos");
                }

                var usuarioAntigo = await _usuarioService.BuscarPorIdAsync(id);
                if (usuarioAntigo == null)
                {
                    return Erro("Usuário não encontrado");
                }

                var usuarioAtualizado = await _usuarioService.AtualizarUsuarioAsync(id, model);

                await RegistraAcaoAsync(
                    "Atualizar Usuário",
                    SerializarParaLog(usuarioAntigo),
                    SerializarParaLog(usuarioAtualizado),
                    $"Usuário {model.Nome} atualizado com sucesso");

                return Sucesso(usuarioAtualizado, "Usuário atualizado com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(AtualizarUsuario), $"ID: {id}, Dados: {SerializarParaLog(model)}");
            }
        }

        /// <summary>
        /// Remove um usuário
        /// </summary>
        /// <param name="id">ID do usuário</param>
        /// <returns>Confirmação da remoção</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoverUsuario(string id)
        {
            try
            {
                var usuario = await _usuarioService.BuscarPorIdAsync(id);
                if (usuario == null)
                {
                    return Erro("Usuário não encontrado");
                }

                await _usuarioService.ExcluirPorIdAsync(id);

                await RegistraAcaoAsync(
                    "Remover Usuário",
                    SerializarParaLog(usuario),
                    "Usuário removido",
                    $"Usuário {usuario.Nome} removido com sucesso");

                return Sucesso(null, "Usuário removido com sucesso");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(RemoverUsuario), $"ID: {id}");
            }
        }

        /// <summary>
        /// Descriptografa uma senha criptografada (uso administrativo)
        /// </summary>
        /// <param name="request">Dados de descriptografia</param>
        /// <returns>Senha em texto plano</returns>
        [AllowAnonymous]
        [HttpPost("descriptografar-senha")]
        public async Task<IActionResult> DescriptografarSenha([FromBody] DescriptografarSenhaRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Erro("Dados inválidos");
                }

                if (string.IsNullOrWhiteSpace(request.SenhaCriptografada))
                {
                    return Erro("Senha criptografada não pode ser vazia");
                }

                var senhaPlana = Criptografia.Desencripitar(request.SenhaCriptografada);

                // Log da operação para auditoria
                await LogInfoAsync(
                    $"Descriptografia de senha solicitada - UsuarioId: {request.UsuarioId ?? "N/A"}",
                    nameof(DescriptografarSenha));

                return Sucesso(new
                {
                    usuarioId = request.UsuarioId,
                    senhaPlana = senhaPlana,
                    senhaCriptografada = request.SenhaCriptografada
                }, "Senha descriptografada com sucesso");
            }
            catch (FormatException ex)
            {
                await LogInfoAsync(
                    $"Erro ao descriptografar senha - formato inválido. UsuarioId: {request.UsuarioId ?? "N/A"}",
                    nameof(DescriptografarSenha));
                return Erro("Senha criptografada está em formato inválido");
            }
            catch (Exception ex)
            {
                return await LogErroAsync(ex, nameof(DescriptografarSenha),
                    $"UsuarioId: {request.UsuarioId ?? "N/A"}");
            }
        }
    }

    /// <summary>
    /// Request para descriptografar senha
    /// </summary>
    public class DescriptografarSenhaRequest
    {
        /// <summary>
        /// ID do usuário (opcional, para fins de auditoria)
        /// </summary>
        public string? UsuarioId { get; set; }

        /// <summary>
        /// Senha criptografada em Base64
        /// </summary>
        public string SenhaCriptografada { get; set; } = string.Empty;
    }
}