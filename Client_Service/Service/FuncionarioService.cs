using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Client_Service.ServiceGenerico;
using Shared.Classes.Entidades.Client;
using Shared.Classes.Entidades.ADM;
using Shared.Utils.Criptografia;
using Admin_Service.Service.Interface;
using Admin_Repository.Repositorio.Interface;

namespace Client_Service.Service
{
    public class FuncionarioService : ServiceGenerico<Funcionario>, IFuncionarioService
    {
        private readonly IFuncionarioRepositorio _funcionarioRepositorio;
        private readonly IUsuarioService _usuarioService;
        private readonly IUsuarioEmpresaService _usuarioEmpresaService;
        private readonly IUsuarioRepository _usuarioRepository;

        public FuncionarioService(
            IFuncionarioRepositorio funcionarioRepositorio,
            IUsuarioService usuarioService,
            IUsuarioEmpresaService usuarioEmpresaService,
            IUsuarioRepository usuarioRepository)
            : base(funcionarioRepositorio)
        {
            _funcionarioRepositorio = funcionarioRepositorio;
            _usuarioService = usuarioService;
            _usuarioEmpresaService = usuarioEmpresaService;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<Funcionario> AdicionarOuEditarFuncionarioComUsuarioAsync(
            Funcionario funcionario,
            string nome,
            string email,
            string cpf,
            string celular,
            string senhaUsuario,
            string empresaId)
        {
            try
            {
                // Validar campos obrigatórios do usuário
                if (string.IsNullOrWhiteSpace(nome))
                    throw new ArgumentException("Nome do funcionário é obrigatório");

                if (string.IsNullOrWhiteSpace(email))
                    throw new ArgumentException("Email do funcionário é obrigatório");

                if (string.IsNullOrWhiteSpace(cpf))
                    throw new ArgumentException("CPF do funcionário é obrigatório");

                if (string.IsNullOrWhiteSpace(empresaId))
                    throw new ArgumentException("ID da empresa é obrigatório");

                // Verificar se é edição ou criação
                bool isEdicao = !string.IsNullOrWhiteSpace(funcionario.Id);

                // Identificar o usuário atual (em caso de edição)
                string? usuarioIdExcluir = null;

                if (isEdicao)
                {
                    var funcionarioExistente = await _funcionarioRepositorio.BuscarPorIdAsync(funcionario.Id);
                    if (funcionarioExistente != null)
                    {
                        usuarioIdExcluir = funcionarioExistente.UsuarioId;
                    }
                }

                // Buscar usuário existente por email ou CPF
                var todosUsuarios = await _usuarioService.BuscarTodosAsync();
                var usuarioComEmailOuCpf = todosUsuarios.FirstOrDefault(u =>
                    (u.Email.ToLower() == email.ToLower() || u.Cpf == cpf) &&
                    u.Id != usuarioIdExcluir);

                // Validar celular (caso fornecido)
                if (!string.IsNullOrWhiteSpace(celular))
                {
                    var usuarioComMesmoCelular = todosUsuarios.FirstOrDefault(u =>
                        u.Celular == celular && u.Id != usuarioIdExcluir);
                    if (usuarioComMesmoCelular != null)
                        throw new InvalidOperationException("Já existe um usuário com este celular");
                }

                string usuarioIdParaVincular;

                // Se encontrou usuário existente, validar se já está vinculado a outro funcionário nesta empresa
                if (usuarioComEmailOuCpf != null)
                {
                    // Buscar se já existe funcionário com este usuário na base atual (multi-tenant)
                    var funcionarioComUsuario = await _funcionarioRepositorio.BuscarPorUsuarioIdAsync(usuarioComEmailOuCpf.Id);

                    // Se encontrou e não é o próprio funcionário sendo editado, retornar erro
                    if (funcionarioComUsuario != null && funcionarioComUsuario.Id != funcionario.Id)
                    {
                        throw new InvalidOperationException("Este usuário já está vinculado a outro funcionário nesta empresa");
                    }

                    // Usuário existe mas não está vinculado a nenhum funcionário (ou está sendo editado)
                    // Reutilizar o ID do usuário existente
                    usuarioIdParaVincular = usuarioComEmailOuCpf.Id;
                }
                else
                {
                    // Usuário não existe, será necessário criar um novo
                    usuarioIdParaVincular = null;
                }

                Funcionario funcionarioSalvo;

                if (isEdicao)
                {
                    // Edição: buscar funcionário existente
                    var funcionarioExistente = await _funcionarioRepositorio.BuscarPorIdAsync(funcionario.Id);
                    if (funcionarioExistente == null)
                        throw new KeyNotFoundException("Funcionário não encontrado");

                    // Atualizar usuário vinculado
                    if (!string.IsNullOrWhiteSpace(funcionarioExistente.UsuarioId))
                    {
                        var usuarioExistente = await _usuarioService.BuscarPorIdAsync(funcionarioExistente.UsuarioId);
                        if (usuarioExistente != null)
                        {
                            usuarioExistente.Nome = nome;
                            usuarioExistente.Email = email;
                            usuarioExistente.Cpf = cpf;
                            usuarioExistente.Celular = celular;

                            // Atualizar senha apenas se fornecida
                            if (!string.IsNullOrWhiteSpace(senhaUsuario))
                            {
                                usuarioExistente.Senha = CriptografiaUtils.GerarHashSenha(senhaUsuario);
                            }

                            usuarioExistente.DtaAlteracao = DateTime.UtcNow;

                            await _usuarioService.EditarAsync(usuarioExistente);
                        }
                    }

                    // Atualizar funcionário
                    funcionario.DtaAlteracao = DateTime.UtcNow;
                    funcionario.DtaCadastro = funcionarioExistente.DtaCadastro;
                    funcionario.UsuarioId = funcionarioExistente.UsuarioId;

                    funcionarioSalvo = await _funcionarioRepositorio.EditarAsync(funcionario);
                }
                else
                {
                    // Criação de novo funcionário
                    string usuarioIdFinal;

                    if (string.IsNullOrWhiteSpace(usuarioIdParaVincular))
                    {
                        // Criar novo usuário
                        if (string.IsNullOrWhiteSpace(senhaUsuario))
                            throw new ArgumentException("Senha do usuário é obrigatória para novo funcionário");

                        var novoUsuario = new Usuario
                        {
                            Nome = nome,
                            Email = email.ToLower(),
                            Cpf = cpf,
                            Celular = celular,
                            Senha = CriptografiaUtils.GerarHashSenha(senhaUsuario),
                            DtaNascimento = DateTime.UtcNow,
                            FlgAtivo = true,
                            FlgInterno = false,
                            FlgAutenticacaoDuasEtapas = false,
                            DtaCadastro = DateTime.UtcNow,
                            DtaAlteracao = DateTime.UtcNow
                        };

                        var usuarioCriado = await _usuarioService.AdicionarAsync(novoUsuario);

                        if (usuarioCriado == null || string.IsNullOrWhiteSpace(usuarioCriado.Id))
                            throw new InvalidOperationException("Erro ao criar usuário para o funcionário");

                        usuarioIdFinal = usuarioCriado.Id;
                    }
                    else
                    {
                        // Reutilizar usuário existente
                        usuarioIdFinal = usuarioIdParaVincular;

                        // Atualizar dados do usuário existente (exceto senha, pois já tem senha)
                        var usuario = await _usuarioService.BuscarPorIdAsync(usuarioIdFinal);
                        if (usuario != null)
                        {
                            usuario.Nome = nome;
                            usuario.Email = email.ToLower();
                            usuario.Cpf = cpf;
                            usuario.Celular = celular;
                            usuario.DtaAlteracao = DateTime.UtcNow;

                            await _usuarioService.EditarAsync(usuario);
                        }
                    }

                    // Verificar se o usuário já está vinculado à empresa
                    var usuarioEmpresaExistente = await _usuarioEmpresaService.BuscarTodosAsync();
                    var vinculoExistente = usuarioEmpresaExistente.FirstOrDefault(ue =>
                        ue.UsuarioId == usuarioIdFinal && ue.EmpresaId == empresaId);

                    if (vinculoExistente == null)
                    {
                        // Vincular usuário à empresa
                        var usuarioEmpresa = new UsuarioEmpresa
                        {
                            UsuarioId = usuarioIdFinal,
                            EmpresaId = empresaId,
                            FlgAtivo = true,
                            DtaCadastro = DateTime.UtcNow,
                            DtaAlteracao = DateTime.UtcNow
                        };

                        await _usuarioEmpresaService.AdicionarAsync(usuarioEmpresa);
                    }

                    // Criar funcionário vinculado ao usuário
                    funcionario.UsuarioId = usuarioIdFinal;
                    funcionario.FlgAtivo = true;
                    funcionario.DtaCadastro = DateTime.UtcNow;
                    funcionario.DtaAlteracao = DateTime.UtcNow;

                    funcionarioSalvo = await _funcionarioRepositorio.AdicionarAsync(funcionario);
                }

                return funcionarioSalvo;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao adicionar/editar funcionário: {ex.Message}", ex);
            }
        }

        public async Task<Funcionario?> BuscarPorUsuarioIdAsync(string usuarioId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuarioId))
                    return null;

                return await _funcionarioRepositorio.BuscarPorUsuarioIdAsync(usuarioId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar funcionário por usuário ID: {ex.Message}", ex);
            }
        }

        public async Task AtualizarHorariosAtendimentoAsync(string funcionarioId, List<HorarioAtendimento> horarios)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(funcionarioId))
                    throw new ArgumentException("ID do funcionário é obrigatório");

                var funcionario = await _funcionarioRepositorio.BuscarPorIdAsync(funcionarioId);
                if (funcionario == null)
                    throw new KeyNotFoundException("Funcionário não encontrado");

                await _funcionarioRepositorio.AtualizarHorariosAtendimentoAsync(funcionarioId, horarios ?? new List<HorarioAtendimento>());
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao atualizar horários de atendimento: {ex.Message}", ex);
            }
        }

        public async Task<List<HorarioAtendimento>> RecuperarHorariosAtendimentoAsync(string funcionarioId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(funcionarioId))
                    throw new ArgumentException("ID do funcionário é obrigatório");

                var funcionario = await _funcionarioRepositorio.BuscarPorIdAsync(funcionarioId);
                if (funcionario == null)
                    throw new KeyNotFoundException("Funcionário não encontrado");

                return funcionario.HorariosAtendimento ?? new List<HorarioAtendimento>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao recuperar horários de atendimento: {ex.Message}", ex);
            }
        }

        public async Task<Funcionario> AlterarStatusFuncionarioAsync(string funcionarioId, bool ativo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(funcionarioId))
                    throw new ArgumentException("ID do funcionário é obrigatório");

                var funcionario = await _funcionarioRepositorio.BuscarPorIdAsync(funcionarioId);
                if (funcionario == null)
                    throw new KeyNotFoundException("Funcionário não encontrado");

                funcionario.FlgAtivo = ativo;
                funcionario.DtaAlteracao = DateTime.UtcNow;

                // Também atualizar o status do usuário vinculado
                if (!string.IsNullOrWhiteSpace(funcionario.UsuarioId))
                {
                    var usuario = await _usuarioService.BuscarPorIdAsync(funcionario.UsuarioId);
                    if (usuario != null)
                    {
                        usuario.FlgAtivo = ativo;
                        usuario.DtaAlteracao = DateTime.UtcNow;
                        await _usuarioService.EditarAsync(usuario);
                    }
                }

                return await _funcionarioRepositorio.EditarAsync(funcionario);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao alterar status do funcionário: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Funcionario>> BuscarFuncionariosAtivosAsync()
        {
            try
            {
                return await _funcionarioRepositorio.BuscarFuncionariosAtivosAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar funcionários ativos: {ex.Message}", ex);
            }
        }

        protected override async Task ValidarEntidade(Funcionario entidade)
        {
            var erros = new List<string>();

            if (string.IsNullOrWhiteSpace(entidade.UsuarioId))
                erros.Add("UsuarioId é obrigatório");

            if (erros.Any())
                throw new ArgumentException($"Validação falhou: {string.Join(", ", erros)}");

            await Task.CompletedTask;
        }
    }
}
