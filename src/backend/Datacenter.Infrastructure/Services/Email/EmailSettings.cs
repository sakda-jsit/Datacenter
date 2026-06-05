namespace Datacenter.Infrastructure.Services.Email;

/// <summary>
/// การตั้งค่า SMTP (อ่านจาก config section "EmailSettings"). เจ้าหน้าที่กรอกค่าจริงตอน deploy.
/// ถ้า Host ว่าง = ยังไม่ตั้งค่า → การส่งจะล้มเหลวพร้อมข้อความชัดเจน.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
}
