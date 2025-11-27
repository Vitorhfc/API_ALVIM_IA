using Admin_Repository.Repositorio.Interface;
using Admin_Service.Service.Interface;
using Admin_Service.ServiceGenerico;
using AutoMapper;
using Shared.Classes.Entidades.ADM;
using Shared.Classes.ModelView.ADM;
using Shared.Utils;
using Shared.Utils.Criptografia;

namespace Admin_Service.Service
{
    public class UsuarioService : ServiceGenerico<Usuario>, IUsuarioService
    {
        #region Campos

        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IMapper _mapper;

        #endregion

        #region Construtor

        public UsuarioService(IUsuarioRepository usuarioRepository, IMapper mapper)
            : base(usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
            _mapper = mapper;
        }

        #endregion

        #region Validações

        public override async Task ValidarEntidadeAsync(Usuario entity)
        {
            ValidarCamposObrigatorios(entity);
            await ValidarCamposUnicosAsync(entity);
        }

        private static void ValidarCamposObrigatorios(Usuario usuario)
        {
            ValidarCampoObrigatorio(usuario.Nome, "Nome");
            ValidarCampoObrigatorio(usuario.Cpf, "CPF");
            ValidarCampoObrigatorio(usuario.Email, "Email");
            ValidarCampoObrigatorio(usuario.Celular, "Celular");

            if (usuario.DtaNascimento == default)
                throw new ArgumentException("Data de nascimento é obrigatória");

            if (!ValidacaoDeDados.ValidarCpf(usuario.Cpf))
                throw new ArgumentException("CPF inválido");

            if (!ValidacaoDeDados.ValidarEmail(usuario.Email))
                throw new ArgumentException("Email inválido");

            if (!ValidacaoDeDados.ValidarTelefoneCelular(usuario.Celular))
                throw new ArgumentException("Celular inválido");
        }

        private async Task ValidarCamposUnicosAsync(Usuario usuario)
        {
            var cpfExistente = await _usuarioRepository.BuscarPrimeiroPorFiltroAsync(u =>
                u.Cpf == usuario.Cpf && u.Id != usuario.Id);

            if (cpfExistente != null)
                throw new InvalidOperationException("CPF já cadastrado no sistema");

            var emailExistente = await _usuarioRepository.BuscarPrimeiroPorFiltroAsync(u =>
                u.Email == usuario.Email && u.Id != usuario.Id);

            if (emailExistente != null)
                throw new InvalidOperationException("Email já cadastrado no sistema");

            var celularExistente = await _usuarioRepository.BuscarPrimeiroPorFiltroAsync(u =>
                u.Celular == usuario.Celular && u.Id != usuario.Id);

            if (celularExistente != null)
                throw new InvalidOperationException("Celular já cadastrado no sistema");
        }

        #endregion

        #region Métodos Públicos

        public async Task<UsuarioModel> CadastrarUsuarioAsync(UsuarioModel model)
        {
            var usuario = _mapper.Map<Usuario>(model);

            usuario.FlgInterno = false;
            usuario.Senha = CriptografiaUtils.GerarHashSenha(model.Senha);
            usuario.DtaCadastro = DateTime.Now;

            await ValidarEntidadeAsync(usuario);

            var usuarioCriado = await _usuarioRepository.AdicionarAsync(usuario);
            return _mapper.Map<UsuarioModel>(usuarioCriado);
        }

        public async Task<UsuarioModel> AtualizarUsuarioAsync(string id, UsuarioModel model)
        {
            var usuarioExistente = await _usuarioRepository.BuscarPorIdAsync(id);

            if (usuarioExistente == null)
                throw new KeyNotFoundException($"Usuário com ID '{id}' não encontrado");

            usuarioExistente.Nome = model.Nome;
            usuarioExistente.Email = model.Email;
            usuarioExistente.Cpf = model.Cpf;
            usuarioExistente.Celular = model.Celular;
            usuarioExistente.DtaNascimento = model.DtaNascimento;

            if (!string.IsNullOrWhiteSpace(model.Senha))
                usuarioExistente.Senha = CriptografiaUtils.GerarHashSenha(model.Senha);

            usuarioExistente.DtaAlteracao = DateTime.Now;

            await ValidarEntidadeAsync(usuarioExistente);

            var usuarioAtualizado = await _usuarioRepository.EditarAsync(usuarioExistente);
            return _mapper.Map<UsuarioModel>(usuarioAtualizado);
        }

        #endregion
    }
}