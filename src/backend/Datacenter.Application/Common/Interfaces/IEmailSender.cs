namespace Datacenter.Application.Common.Interfaces;

/// <summary>ไฟล์แนบอีเมล</summary>
public record EmailAttachment(string FileName, byte[] Content, string ContentType = "application/pdf");

/// <summary>ข้อความอีเมลพร้อมไฟล์แนบ</summary>
public record EmailMessage(
    string To,
    string Subject,
    string BodyHtml,
    IReadOnlyList<EmailAttachment> Attachments);

/// <summary>
/// ส่งอีเมล (SMTP). throw เมื่อยังไม่ได้ตั้งค่า SMTP หรือส่งไม่สำเร็จ — ผู้เรียกจับ exception
/// เพื่อบันทึกสถานะ "ส่งไม่สำเร็จ" พร้อมข้อความ error.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
