namespace Shared.Enumeradores
{
    public class Enumeradores
    {
        public enum TomVoz
        {
            Masculino = 1,
            Feminino = 2
        }

        public enum StatusConversa
        {
            Ativa = 1,
            EmAtendimentoHumano = 2,
            Finalizada = 3,
            Aguardando = 4
        }

        public enum TipoMensagem
        {
            Texto = 1,
            Audio = 2,
            Imagem = 3,
            Video = 4,
            Documento = 5,
            Contato = 6,
            Localizacao = 7,
            Sticker = 8
        }

        public enum OrigemMensagem
        {
            Cliente = 1,
            IA = 2,
            Funcionario = 3
        }

        public enum StatusEntrega
        {
            Enviada = 1,
            Entregue = 2,
            Lida = 3,
            Falha = 4
        }
        public enum StatusProcessamento
        {
            Sucesso = 1,
            Erro = 2,
            Timeout = 3
        }

        public enum CodigoModulo
        {
            Base = 1,
            Documentos = 2,
            Agendamento = 3,
            Audio = 4
        }

        /// <summary>
        /// Tipo de log
        /// </summary>
        public enum TipoLog
        {
            Sistema = 1,
            Integracao = 2,
            Processamento = 3,
            Erro = 4,
            Webhook = 5
        }

        /// <summary>
        /// Nível de severidade do log
        /// </summary>
        public enum NivelSeveridade
        {
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        /// <summary>
        /// Preferência de resposta da IA
        /// </summary>
        public enum PreferenciaResposta
        {
            SomenteTexto = 1,
            PreferenciaAudio = 2,
            Misto = 3
        }
    }
}
