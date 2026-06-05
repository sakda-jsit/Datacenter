using System.Net;
using System.Net.Mail;
using Datacenter.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Datacenter.Infrastructure.Services.Email;

/// <summary>ส่งอีเมลผ่าน SMTP ด้วย System.Net.Mail (ไม่ต้องพึ่ง package ภายนอก)</summary>
public class SmtpEmailSender(IOptions<EmailSettings> options) : IEmailSender
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
            throw new InvalidOperationException(
                "ยังไม่ได้ตั้งค่า SMTP (EmailSettings:Host / FromAddress) — โปรดตั้งค่าในไฟล์ appsettings หรือ environment variables");

        if (string.IsNullOrWhiteSpace(message.To))
            throw new InvalidOperationException("ไม่พบอีเมลผู้รับ");

        using var mail = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, string.IsNullOrWhiteSpace(_settings.FromName) ? _settings.FromAddress : _settings.FromName),
            Subject = message.Subject,
            Body = message.BodyHtml,
            IsBodyHtml = true,
        };
        mail.To.Add(message.To);

        foreach (var att in message.Attachments)
            mail.Attachments.Add(new Attachment(new MemoryStream(att.Content), att.FileName, att.ContentType));

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
        if (!string.IsNullOrWhiteSpace(_settings.User))
            client.Credentials = new NetworkCredential(_settings.User, _settings.Password);

        await client.SendMailAsync(mail, ct);
    }
}
