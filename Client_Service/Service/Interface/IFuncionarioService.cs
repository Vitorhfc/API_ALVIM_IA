using Client_Service.ServiceGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service.Interface
{
    public interface IFuncionarioService : IServiceGenerico<Funcionario>
    {
        /// <summary>
        /// Adiciona ou edita um funcionário e cria/atualiza seu usuário vinculado
        /// </summary>
        Task<Funcionario> AdicionarOuEditarFuncionarioComUsuarioAsync(
            Funcionario funcionario,
            string nome,
            string email,
            string cpf,
            string celular,
            string senhaUsuario,
            string empresaId);

        /// <summary>
        /// Busca funcionário por ID do usuário
        /// </summary>
        Task<Funcionario?> BuscarPorUsuarioIdAsync(string usuarioId);

        /// <summary>
        /// Atualiza horários de atendimento do funcionário
        /// </summary>
        Task AtualizarHorariosAtendimentoAsync(string funcionarioId, List<HorarioAtendimento> horarios);

        /// <summary>
        /// Recupera os horários de atendimento do funcionário
        /// </summary>
        Task<List<HorarioAtendimento>> RecuperarHorariosAtendimentoAsync(string funcionarioId);

        /// <summary>
        /// Inativa ou ativa funcionário
        /// </summary>
        Task<Funcionario> AlterarStatusFuncionarioAsync(string funcionarioId, bool ativo);

        /// <summary>
        /// Busca todos os funcionários ativos
        /// </summary>
        Task<IEnumerable<Funcionario>> BuscarFuncionariosAtivosAsync();
    }
}
