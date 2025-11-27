using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Classes.Model;
using Shared.Services.Interface;
using System.Text;

namespace Shared.Services
{
    public class EmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmailService> _logger;

        private const string API_KEY = "re_fGJvfAd9_8drKS3vLyFWofxX5uX14fjwf";
        private const string API_URL = "https://api.resend.com/emails";
        private const string EMAIL_PADRAO = "naoresponda@empreflow.com.br";
        private const string NOME_PADRAO = "Não Responda";

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
        }

        public async Task<EnvioResponse> EnviarEmailAsync(EnviarEmailRequest request)
        {
            try
            {
                if (request?.Destinatarios == null || !request.Destinatarios.Any())
                {
                    return new EnvioResponse
                    {
                        Sucesso = false,
                        Mensagem = "Lista de destinatários vazia",
                        DataEnvio = DateTime.UtcNow,
                        Erro = "Nenhum destinatário informado"
                    };
                }

                var emailRemetente = string.IsNullOrEmpty(request.EmailRemetente)
                    ? EMAIL_PADRAO
                    : request.EmailRemetente;

                var nomeRemetente = string.IsNullOrEmpty(request.NomeRemetente)
                    ? NOME_PADRAO
                    : request.NomeRemetente;

                foreach (var destinatario in request.Destinatarios.Distinct())
                {
                    if (string.IsNullOrEmpty(destinatario))
                        continue;

                    var payload = new
                    {
                        from = $"{nomeRemetente} <{emailRemetente}>",
                        to = new[] { destinatario },
                        subject = request.Assunto,
                        html = request.CorpoHTML ? request.Corpo : $"<p>{request.Corpo}</p>",
                        bcc = request.DestinatariosCopiaOculta?
                            .Distinct()
                            .Where(bcc => !string.IsNullOrEmpty(bcc))
                            .ToArray()
                    };

                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(API_URL, content);
                    response.EnsureSuccessStatusCode();
                }

                return new EnvioResponse
                {
                    Sucesso = true,
                    Mensagem = "Email(s) enviado(s) com sucesso",
                    DataEnvio = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar emails");

                return new EnvioResponse
                {
                    Sucesso = false,
                    Mensagem = "Erro ao processar envio de emails",
                    DataEnvio = DateTime.UtcNow,
                    Erro = ex.Message
                };
            }
        }

        public async Task<EnvioResponse> EnviarEmailSimplesAsync(
            string destinatario,
            string assunto,
            string corpo)
        {
            return await EnviarEmailAsync(new EnviarEmailRequest
            {
                Destinatarios = new List<string> { destinatario },
                Assunto = assunto,
                Corpo = corpo,
                CorpoHTML = false
            });
        }

        public async Task<EnvioResponse> EnviarEmailHTMLAsync(
            string destinatario,
            string assunto,
            string corpoHTML)
        {
            return await EnviarEmailAsync(new EnviarEmailRequest
            {
                Destinatarios = new List<string> { destinatario },
                Assunto = assunto,
                Corpo = corpoHTML,
                CorpoHTML = true
            });
        }

        public async Task<EnvioResponse> EnviarEmailMultiplosDestinatariosAsync(
            List<string> destinatarios,
            string assunto,
            string corpo,
            bool html = true)
        {
            return await EnviarEmailAsync(new EnviarEmailRequest
            {
                Destinatarios = destinatarios,
                Assunto = assunto,
                Corpo = corpo,
                CorpoHTML = html
            });
        }
    }
}