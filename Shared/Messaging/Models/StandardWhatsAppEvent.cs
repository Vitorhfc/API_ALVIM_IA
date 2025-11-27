namespace Shared.Messaging.Models
{
    /// <summary>
    /// Evento padronizado de WhatsApp extraído do webhook
    /// </summary>
    public class StandardWhatsAppEvent
    {
        /// <summary>
        /// Informações do contato que enviou a mensagem
        /// </summary>
        public ContactInfo ContactInfo { get; set; }

        /// <summary>
        /// ID da mensagem no WhatsApp
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Timestamp da mensagem
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Corpo da mensagem (texto)
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Tipo de evento (message, message.ack, etc)
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Se a mensagem é do próprio usuário
        /// </summary>
        public bool FromMe { get; set; }
    }

    /// <summary>
    /// Informações do contato do WhatsApp
    /// </summary>
    public class ContactInfo
    {
        /// <summary>
        /// Número do telefone (pode ser JID ou número limpo)
        /// Ex: 5541999887766@c.us ou 5541999887766
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Nome do contato (pushName ou notifyName)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Se é um grupo
        /// </summary>
        public bool IsGroup { get; set; }
    }
}
