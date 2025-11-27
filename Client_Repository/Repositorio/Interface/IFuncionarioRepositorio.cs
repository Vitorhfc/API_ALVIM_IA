using Client_Repository.RepositorioGenerico.Interface;
using Shared.Classes.Entidades.Client;

namespace Client_Repository.Repositorio.Interface
{
    /// <summary>
    /// Interface do repositório de Funcionário - Métodos Adicionais
    /// </summary>
    public interface IFuncionarioRepositorio : IRepositorioGenerico<Funcionario>
    {
        /// <summary>
        /// Busca funcionário por ID do usuário vinculado
        /// </summary>
        Task<Funcionario?> BuscarPorUsuarioIdAsync(string usuarioId);

        /// <summary>
        /// Atualiza os horários de atendimento do funcionário
        /// </summary>
        Task AtualizarHorariosAtendimentoAsync(string funcionarioId, List<HorarioAtendimento> horarios);

        /// <summary>
        /// Busca funcionários ativos
        /// </summary>
        Task<IEnumerable<Funcionario>> BuscarFuncionariosAtivosAsync();
    }
}
