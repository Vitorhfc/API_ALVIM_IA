using Client_Repository.Repositorio.Interface;
using Client_Service.Service.Interface;
using Client_Service.ServiceGenerico;
using Shared.Classes.Entidades.Client;
using Shared.Utils;


namespace Client_Service.Service
{
    public class MensagemService : ServiceGenerico<Mensagem>, IMensagemService
    {
        private readonly IMensagemRepositorio _mensagemRepositorio;

        public MensagemService(IMensagemRepositorio repositorio)
            : base(repositorio)
        {
            _mensagemRepositorio = repositorio;
        }

        public async Task<IEnumerable<Mensagem>> BuscarPorClienteAsync(string clienteId)
        {
            try
            {
                // ✅ CORRIGIDO: conversaId → clienteId
                if (string.IsNullOrWhiteSpace(clienteId))
                    throw new ArgumentException("ID do cliente não pode ser vazio", nameof(clienteId));

                // ✅ CORRIGIDO: ConversaId → ClienteId
                return await _mensagemRepositorio.BuscarPorFiltroAsync(m => m.ClienteId == clienteId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar mensagens por cliente: {ex.Message}", ex);
            }
        }
        public async Task<IEnumerable<Mensagem>> BuscarPorRemetenteAsync(string remetenteId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(remetenteId))
                    throw new ArgumentException("ID do remetente não pode ser vazio", nameof(remetenteId));

                return await _mensagemRepositorio.BuscarPorFiltroAsync(m => m.ClienteId == remetenteId);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar mensagens por remetente: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Mensagem>> BuscarNaoLidasAsync(string clienteId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clienteId))
                    throw new ArgumentException("ID do cliente não pode ser vazio", nameof(clienteId));
                return await _mensagemRepositorio.BuscarPorFiltroAsync(m =>
                    m.ClienteId == clienteId &&
                    m.StatusEntrega != StatusEntrega.Lida);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao buscar mensagens não lidas: {ex.Message}", ex);
            }
        }

        public async Task<int> ContarNaoLidasAsync(string conversaId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(conversaId))
                    throw new ArgumentException("ID da conversa não pode ser vazio", nameof(conversaId));

                var mensagens = await BuscarNaoLidasAsync(conversaId);
                return mensagens.Count();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Erro ao contar mensagens não lidas: {ex.Message}", ex);
            }
        }

        protected override async Task ValidarEntidade(Mensagem entidade)
        {
            var erros = new List<string>();

            if (string.IsNullOrWhiteSpace(entidade.ClienteId))
                erros.Add("Cliente é obrigatório");

            if (entidade.TipoMensagem == TipoMensagem.Texto &&
                string.IsNullOrWhiteSpace(entidade.ConteudoTexto))
            {
                erros.Add("Conteúdo de texto é obrigatório para mensagens do tipo Texto");
            }

            if (string.IsNullOrWhiteSpace(entidade.IdMensagemWhatsApp))
                erros.Add("ID da mensagem do WhatsApp é obrigatório");

            if (!Enum.IsDefined(typeof(TipoMensagem), entidade.TipoMensagem))
                erros.Add("Tipo de mensagem inválido");
            
            if (entidade.TipoMensagem != TipoMensagem.Texto &&
                entidade.TipoMensagem != TipoMensagem.Contato &&
                entidade.TipoMensagem != TipoMensagem.Localizacao)
            {
                if (entidade.Midia == null ||
                    string.IsNullOrWhiteSpace(entidade.Midia.UrlDownload))
                {
                    erros.Add($"Mídia é obrigatória para mensagens do tipo {EnumHelper.EnumToString(entidade.TipoMensagem)}");
                }
            }

            if (erros.Any())
                throw new ArgumentException($"Validação falhou: {string.Join(", ", erros)}");

            await Task.CompletedTask;
        }
    }
}