namespace Shared.Templates
{
    /// <summary>
    /// Templates de mensagens para comunicação com usuários
    /// </summary>
    public static class TemplatesMensagens
    {
        #region Templates 2FA - Email

        /// <summary>
        /// Template de email para código 2FA
        /// </summary>
        public static string EmailCodigo2FA(string nomeUsuario, string codigo, int validadeMinutos = 10)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .code-box {{ background: white; border: 2px dashed #667eea; border-radius: 10px; padding: 20px; text-align: center; margin: 20px 0; }}
        .code {{ font-size: 32px; font-weight: bold; color: #667eea; letter-spacing: 8px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Código de Verificação</h1>
        </div>
        <div class='content'>
            <p>Olá <strong>{nomeUsuario}</strong>,</p>

            <p>Você solicitou acesso ao sistema <strong>Alvim Atendimento IA</strong>.</p>

            <p>Use o código abaixo para completar sua autenticação:</p>

            <div class='code-box'>
                <div class='code'>{codigo}</div>
            </div>

            <div class='warning'>
                <p><strong>⚠️ Importante:</strong></p>
                <ul>
                    <li>Este código é válido por <strong>{validadeMinutos} minutos</strong></li>
                    <li>Não compartilhe este código com ninguém</li>
                    <li>Se você não solicitou este código, ignore este email</li>
                </ul>
            </div>

            <p>Se você tiver alguma dúvida, entre em contato com nosso suporte.</p>

            <p>Atenciosamente,<br><strong>Equipe Alvim Atendimento IA</strong></p>
        </div>
        <div class='footer'>
            <p>Este é um email automático, por favor não responda.</p>
            <p>&copy; {DateTime.Now.Year} Alvim Atendimento IA. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Assunto do email de código 2FA
        /// </summary>
        public static string EmailCodigo2FAAssunto => "🔐 Seu código de verificação - Alvim";

        #endregion

        #region Templates 2FA - WhatsApp

        /// <summary>
        /// Template de mensagem WhatsApp para código 2FA
        /// </summary>
        public static string WhatsAppCodigo2FA(string nomeUsuario, string codigo, int validadeMinutos = 10)
        {
            return $@"🔐 *Alvim Atendimento IA*

Olá *{nomeUsuario}*!

Seu código de verificação é:

🔢 *{codigo}*

⏱️ Válido por {validadeMinutos} minutos

⚠️ *Importante:*
• Não compartilhe este código
• Use-o apenas no site oficial
• Se não solicitou, ignore esta mensagem

_Esta é uma mensagem automática._";
        }

        #endregion

        #region Templates - Boas-vindas

        /// <summary>
        /// Email de boas-vindas após cadastro
        /// </summary>
        public static string EmailBoasVindas(string nomeUsuario, string emailUsuario)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 15px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .features {{ background: white; padding: 20px; border-radius: 10px; margin: 20px 0; }}
        .feature-item {{ padding: 10px 0; border-bottom: 1px solid #eee; }}
        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Bem-vindo ao Alvim!</h1>
        </div>
        <div class='content'>
            <p>Olá <strong>{nomeUsuario}</strong>,</p>

            <p>É um prazer recebê-lo(a) no <strong>Alvim Atendimento IA</strong>!</p>

            <p>Sua conta foi criada com sucesso e você já pode começar a usar nossa plataforma de atendimento inteligente via WhatsApp.</p>

            <div class='features'>
                <h3>✨ O que você pode fazer:</h3>
                <div class='feature-item'>
                    <strong>🤖 Configurar sua IA</strong><br>
                    Personalize o comportamento do assistente virtual
                </div>
                <div class='feature-item'>
                    <strong>📱 Conectar WhatsApp</strong><br>
                    Integre seu número de atendimento
                </div>
                <div class='feature-item'>
                    <strong>📊 Acompanhar métricas</strong><br>
                    Veja estatísticas de atendimento em tempo real
                </div>
                <div class='feature-item'>
                    <strong>📄 Upload de documentos</strong><br>
                    Adicione materiais para consulta da IA
                </div>
            </div>

            <p>Seus dados de acesso:</p>
            <ul>
                <li><strong>Email:</strong> {emailUsuario}</li>
                <li><strong>Senha:</strong> A que você cadastrou</li>
            </ul>

            <p style='text-align: center;'>
                <a href='#' class='button'>Acessar Plataforma</a>
            </p>

            <p>Se precisar de ajuda, nossa equipe está à disposição!</p>

            <p>Atenciosamente,<br><strong>Equipe Alvim</strong></p>
        </div>
        <div class='footer'>
            <p>Este é um email automático, por favor não responda.</p>
            <p>&copy; {DateTime.Now.Year} Alvim Atendimento IA. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string EmailBoasVindasAssunto => "🎉 Bem-vindo ao Alvim Atendimento IA!";

        #endregion

        #region Templates - Recuperação de Senha

        /// <summary>
        /// Email de recuperação de senha
        /// </summary>
        public static string EmailRecuperacaoSenha(string nomeUsuario, string token, int validadeMinutos = 30)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 15px 30px; background: #f5576c; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔑 Recuperação de Senha</h1>
        </div>
        <div class='content'>
            <p>Olá <strong>{nomeUsuario}</strong>,</p>

            <p>Recebemos uma solicitação para redefinir a senha da sua conta no <strong>Alvim Atendimento IA</strong>.</p>

            <p style='text-align: center;'>
                <a href='#' class='button'>Redefinir Senha</a>
            </p>

            <p>Ou copie e cole o link abaixo no seu navegador:</p>
            <p style='word-break: break-all; background: white; padding: 10px; border-radius: 5px; font-size: 12px;'>
                https://app.alvim.com/recuperar-senha?token={token}
            </p>

            <div class='warning'>
                <p><strong>⚠️ Importante:</strong></p>
                <ul>
                    <li>Este link é válido por <strong>{validadeMinutos} minutos</strong></li>
                    <li>Se você não solicitou esta recuperação, ignore este email</li>
                    <li>Sua senha atual permanece ativa até que você a altere</li>
                </ul>
            </div>

            <p>Atenciosamente,<br><strong>Equipe Alvim</strong></p>
        </div>
        <div class='footer'>
            <p>Este é um email automático, por favor não responda.</p>
            <p>&copy; {DateTime.Now.Year} Alvim Atendimento IA. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string EmailRecuperacaoSenhaAssunto => "🔑 Recuperação de senha - Alvim";

        #endregion
    }
}
