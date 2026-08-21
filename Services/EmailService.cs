using System.Net;
using System.Net.Mail;

namespace MissaoBackend.Services;

// Envio de emails simples via SMTP (ex.: Gmail com password de aplicação).
// Configuração via variáveis de ambiente — se não estiverem definidas, o envio
// é silenciosamente ignorado (fica só um aviso na consola): nunca deve bloquear
// nem falhar a operação principal (ex.: criar uma encomenda) por falta de email.
public static class EmailService
{
    public static async Task EnviarAsync(IConfiguration config, string destinatario, string assunto, string corpoHtml)
    {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? config["Smtp:Host"];
        var portaTxt = Environment.GetEnvironmentVariable("SMTP_PORT") ?? config["Smtp:Port"];
        var utilizador = Environment.GetEnvironmentVariable("SMTP_USER") ?? config["Smtp:User"];
        var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? config["Smtp:Password"];
        var remetenteNome = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? config["Smtp:FromName"] ?? "Ndatava";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(utilizador) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("⚠ Email não enviado (SMTP não configurado): " + assunto);
            return;
        }

        var porta = int.TryParse(portaTxt, out var p) ? p : 587;

        try
        {
            using var cliente = new SmtpClient(host, porta)
            {
                Credentials = new NetworkCredential(utilizador, password),
                EnableSsl = true,
            };

            using var mensagem = new MailMessage
            {
                From = new MailAddress(utilizador, remetenteNome),
                Subject = assunto,
                Body = corpoHtml,
                IsBodyHtml = true,
            };
            mensagem.To.Add(destinatario);

            await cliente.SendMailAsync(mensagem);
        }
        catch (Exception ex)
        {
            // Nunca deixa uma falha de email rebentar a operação principal.
            Console.WriteLine($"⚠ Falha ao enviar email para {destinatario}: {ex.Message}");
        }
    }
}
