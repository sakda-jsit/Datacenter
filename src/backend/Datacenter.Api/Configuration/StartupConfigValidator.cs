namespace Datacenter.Api.Configuration;

/// <summary>
/// ตรวจค่าตั้งที่ขาดไม่ได้ตอนสตาร์ตระบบ — ถ้าไม่ครบให้ล้มทันทีพร้อมบอกวิธีแก้
/// ดีกว่าเปิดใช้งานด้วยกุญแจตัวอย่าง (ซึ่งใครอ่านซอร์สได้ก็ปลอม token เป็นผู้ดูแลระบบได้).
/// </summary>
public static class StartupConfigValidator
{
    /// <summary>ความยาวขั้นต่ำของกุญแจ HMAC-SHA256 (256 บิต)</summary>
    private const int MinJwtKeyLength = 32;

    /// <summary>กุญแจตัวอย่างที่เคยอยู่ในซอร์ส — ห้ามใช้จริง</summary>
    private static readonly string[] ForbiddenKeys =
    [
        "CHANGE_THIS_SECRET_KEY_MIN_32_CHARACTERS_LONG",
    ];

    public static void Validate(IConfiguration configuration, bool isDevelopment)
    {
        var problems = new List<string>();

        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            problems.Add("ไม่ได้ตั้ง ConnectionStrings:DefaultConnection " +
                         "(ตั้งผ่าน environment variable ConnectionStrings__DefaultConnection หรือ appsettings.Local.json)");

        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            problems.Add("ไม่ได้ตั้ง Jwt:Key (ตั้งผ่าน environment variable Jwt__Key หรือ appsettings.Local.json) — " +
                         "สร้างค่าสุ่มด้วย PowerShell: " +
                         "[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }))");
        else
        {
            if (jwtKey.Length < MinJwtKeyLength)
                problems.Add($"Jwt:Key สั้นเกินไป ({jwtKey.Length} ตัวอักษร) — ต้องยาวอย่างน้อย {MinJwtKeyLength} ตัวอักษร");
            if (ForbiddenKeys.Contains(jwtKey))
                problems.Add("Jwt:Key ยังเป็นค่าตัวอย่างในซอร์สโค้ด — ต้องเปลี่ยนเป็นค่าสุ่มเฉพาะของระบบนี้");
        }

        // production: กันเผลอ deploy โดยเปิด CORS ให้ localhost หรือใช้บัญชี sa ของ SQL Server
        if (!isDevelopment && !string.IsNullOrWhiteSpace(connection))
        {
            if (connection.Contains("User ID=sa", StringComparison.OrdinalIgnoreCase)
                || connection.Contains("User Id=sa", StringComparison.OrdinalIgnoreCase)
                || connection.Contains("uid=sa", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine("[คำเตือน] connection string ใช้บัญชี sa ของ SQL Server — " +
                                  "ควรสร้าง login เฉพาะฐานข้อมูลนี้ที่มีสิทธิ์เท่าที่จำเป็น");
        }

        if (problems.Count == 0) return;

        var message = "ค่าตั้งระบบไม่ครบ จึงไม่สามารถเริ่มระบบได้:" + Environment.NewLine
                      + string.Join(Environment.NewLine, problems.Select(p => "  - " + p));
        throw new InvalidOperationException(message);
    }
}
