using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Client_Service.ServiceGenerico;
using Shared.Classes.Entidades.Client;

namespace Client_Service.Service
{
    public class ClienteService : ServiceGenerico<Cliente>, IClienteService
    {
        private readonly IClienteRepositorio _clienteRepositorio;

        public ClienteService(IClienteRepositorio repositorio)
            : base(repositorio)
        {
            _clienteRepositorio = repositorio;
        }

        public async Task<Cliente?> BuscarPorTelefoneAsync(string telefone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(telefone))
                    throw new ArgumentException("Telefone não pode ser vazio", nameof(telefone));

                var telefoneLimpo = telefone.Replace("(", "").Replace(")", "")
                                           .Replace("-", "").Replace(" ", "");

                return await _clienteRepositorio.BuscarPrimeiroPorFiltroAsync(c => c.Numero == telefoneLimpo);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar cliente por telefone: {ex.Message}", ex);
            }
        }

        public async Task<Cliente?> BuscarPorEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    throw new ArgumentException("Email não pode ser vazio", nameof(email));

                var emailLower = email.ToLower();
                return await _clienteRepositorio.BuscarPrimeiroPorFiltroAsync(c => c.Email == emailLower);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar cliente por email: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Cliente>> BuscarClientesAtivosPorTermoAsync(string termo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termo))
                    return Enumerable.Empty<Cliente>();

                var termoLower = termo.ToLower();

                return await _clienteRepositorio.BuscarPorFiltroAsync(c =>
                    c.FlgAtivo == true &&
                    (c.Nome.ToLower().Contains(termoLower) ||
                     (c.Email != null && c.Email.ToLower().Contains(termoLower)) ||
                     (c.Numero != null && c.Numero.Contains(termoLower))));
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar clientes ativos por termo: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidarTelefoneUnicoAsync(string telefone, string? idExcluir = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(telefone))
                    return false;

                var telefoneLimpo = telefone.Replace("(", "").Replace(")", "")
                                           .Replace("-", "").Replace(" ", "");

                var clienteExistente = await _clienteRepositorio.BuscarPrimeiroPorFiltroAsync(c => c.Numero == telefoneLimpo);

                if (clienteExistente == null)
                    return true;

                if (!string.IsNullOrEmpty(idExcluir) && clienteExistente.Id == idExcluir)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao validar telefone único: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidarEmailUnicoAsync(string email, string? idExcluir = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return true;

                var emailLower = email.ToLower();
                var clienteExistente = await _clienteRepositorio.BuscarPrimeiroPorFiltroAsync(c => c.Email == emailLower);

                if (clienteExistente == null)
                    return true;

                if (!string.IsNullOrEmpty(idExcluir) && clienteExistente.Id == idExcluir)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao validar email único: {ex.Message}", ex);
            }
        }

        protected override async Task ValidarEntidade(Cliente entidade)
        {
            var erros = new List<string>();

            if (string.IsNullOrWhiteSpace(entidade.Nome))
                erros.Add("Nome é obrigatório");

            if (string.IsNullOrWhiteSpace(entidade.Numero))
                erros.Add("Telefone é obrigatório");

            if (entidade.Numero != null && entidade.Numero.Length < 10)
                erros.Add("Telefone deve ter no mínimo 10 dígitos");

            if (!string.IsNullOrWhiteSpace(entidade.Email))
            {
                if (!entidade.Email.Contains("@") || !entidade.Email.Contains("."))
                    erros.Add("Email inválido");
            }

            if (erros.Any())
                throw new ArgumentException($"Validação falhou: {string.Join(", ", erros)}");

            await Task.CompletedTask;
        }
    }
}