using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Datacenter.Infrastructure.Services.Dbf;

/// <summary>
/// หนึ่งระเบียนของไฟล์ DBF เข้าถึงค่าฟิลด์ด้วยชื่อ (ไม่สนตัวพิมพ์ใหญ่เล็ก) คล้าย rec[field]
/// </summary>
public sealed class DbfRow(IReadOnlyDictionary<string, object?> values)
{
    public object? this[string field]
        => values.TryGetValue(field, out var v) ? v : null;

    /// <summary>ชื่อฟิลด์ทั้งหมดในระเบียนนี้</summary>
    public IEnumerable<string> Fields => values.Keys;
}

/// <summary>
/// ตัวอ่านไฟล์ DBF ที่เขียนเองเพื่อรองรับฟอร์แมตของ Express (dBASE Level 7 / Visual FoxPro)
/// รองรับชนิดฟิลด์: C, N, F, O (Double), B (VFP Double), I, +, Y (Currency), D, @, T, L, M
///
/// เหตุผลที่ไม่ใช้ dBASE.NET: ไลบรารีนั้นไม่มี encoder สำหรับฟิลด์ชนิด Double ('O')
/// ที่ Express ใช้เก็บยอดเงิน ทำให้ throw ทันทีที่อ่าน — ตัวอ่านนี้ถอดค่าตามสเปก dBASE7 โดยตรง
/// อ้างอิงตรรกะการถอดค่าจาก dbfread (ตัวที่ reference Python ใช้)
/// </summary>
public static class DbfReader
{
    private const byte HeaderTerminator = 0x0D;
    private const byte DeletedFlag = 0x2A; // '*'

    public static List<DbfRow> Read(string path, Encoding textEncoding)
    {
        // เปิดด้วย FileShare.ReadWrite เพื่ออ่านได้แม้โปรแกรม Express จะเปิดไฟล์ค้างไว้
        // (ไฟล์ควบคุมอย่าง ISPRD มักถูก Express ล็อกขณะโปรแกรมเปิดอยู่)
        var bytes = ReadAllBytesShared(path);
        if (bytes.Length < 32)
            throw new InvalidDataException($"ไฟล์ DBF เสียหายหรือว่างเปล่า: {path}");

        int recordCount  = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4));
        int headerLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8, 2));
        int recordLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(10, 2));

        var fields = ParseFields(bytes, textEncoding);

        var rows = new List<DbfRow>(Math.Max(0, recordCount));
        for (int i = 0; i < recordCount; i++)
        {
            int start = headerLength + i * recordLength;
            if (start + recordLength > bytes.Length) break; // ไฟล์ถูกตัดสั้น

            var record = bytes.AsSpan(start, recordLength);
            if (record[0] == DeletedFlag) continue; // ข้ามระเบียนที่ถูกลบ (เหมือน dbfread)

            var values = new Dictionary<string, object?>(fields.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var f in fields)
            {
                var raw = record.Slice(f.Offset, f.Length);
                values[f.Name] = DecodeField(f, raw, textEncoding);
            }
            rows.Add(new DbfRow(values));
        }

        return rows;
    }

    /// <summary>
    /// อ่านไฟล์ทั้งหมดด้วย FileShare.ReadWrite — รองรับกรณีไฟล์ DBF ถูกโปรแกรมอื่น (Express) เปิดค้างไว้
    /// </summary>
    private static byte[] ReadAllBytesShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var buffer = new byte[fs.Length];
        fs.ReadExactly(buffer);
        return buffer;
    }

    // ─── header / field descriptors ────────────────────────────────────────────

    private sealed record FieldDef(string Name, char Type, int Offset, int Length);

    private static List<FieldDef> ParseFields(byte[] bytes, Encoding textEncoding)
    {
        var fields = new List<FieldDef>();
        int pos = 32;       // field descriptors เริ่มหลัง header 32 ไบต์
        int offset = 1;     // ไบต์แรกของระเบียนคือ deletion flag

        while (pos + 1 < bytes.Length && bytes[pos] != HeaderTerminator)
        {
            // ชื่อฟิลด์: 11 ไบต์ ASCII ปิดท้ายด้วย null
            int nameLen = 0;
            while (nameLen < 11 && bytes[pos + nameLen] != 0) nameLen++;
            var name = Encoding.ASCII.GetString(bytes, pos, nameLen);

            char type = (char)bytes[pos + 11];
            int length = bytes[pos + 16];

            fields.Add(new FieldDef(name, type, offset, length));
            offset += length;
            pos += 32;
        }

        return fields;
    }

    // ─── field decoders ──────────────────────────────────────────────────────────

    private static object? DecodeField(FieldDef f, ReadOnlySpan<byte> raw, Encoding textEncoding)
    {
        switch (f.Type)
        {
            case 'C': // Character
                return textEncoding.GetString(raw).TrimEnd('\0', ' ').Trim();

            case 'N': // Numeric (ASCII)
            case 'F': // Float (ASCII)
            {
                var s = Encoding.ASCII.GetString(raw).Trim();
                if (s.Length == 0 || s.All(c => c == '*')) return null;
                return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                    ? d : (decimal?)null;
            }

            case 'O': // Double (dBASE 7) — เก็บแบบ big-endian order-preserving
                return raw.Length >= 8 ? ReadDbase7Double(raw) : null;

            case 'B': // Double (Visual FoxPro) — little-endian IEEE 754
                return raw.Length >= 8 ? BinaryPrimitives.ReadDoubleLittleEndian(raw) : null;

            case 'I': // Integer 4 ไบต์ little-endian
            case '+': // Autoincrement
                return raw.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(raw) : null;

            case 'Y': // Currency — int64 little-endian หาร 10000
                return raw.Length >= 8
                    ? BinaryPrimitives.ReadInt64LittleEndian(raw) / 10000m
                    : (decimal?)null;

            case 'L': // Logical
            {
                char c = (char)raw[0];
                return c is 'T' or 't' or 'Y' or 'y' ? true
                     : c is 'F' or 'f' or 'N' or 'n' ? false
                     : (bool?)null;
            }

            case 'D': // Date YYYYMMDD (ASCII)
            {
                var s = Encoding.ASCII.GetString(raw).Trim();
                return DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt) ? dt : (DateTime?)null;
            }

            case '@': // Timestamp / DateTime (8 ไบต์: julian day + milliseconds)
            case 'T':
                return ReadDbfDateTime(raw);

            case 'M': // Memo — ไม่อ่านไฟล์ memo แยก (.dbt/.fpt); ฟิลด์ที่ใช้งานเป็น Character
                return null;

            default:
                // ชนิดที่ไม่รู้จัก: คืนเป็นข้อความดิบเพื่อไม่ให้ throw ทั้งไฟล์
                return textEncoding.GetString(raw).Trim();
        }
    }

    /// <summary>
    /// ถอดค่า Double ของ dBASE 7 (ฟิลด์ชนิด 'O') ซึ่งเก็บแบบ big-endian
    /// และผ่านการแปลงบิตให้เรียงลำดับได้ (order-preserving):
    ///   - ค่าบวก: เซ็ตบิตบนสุด  → ตอนถอดให้ XOR บิตบนสุดกลับ
    ///   - ค่าลบ : กลับทุกบิต     → ตอนถอดให้กลับทุกบิต
    /// </summary>
    private static double ReadDbase7Double(ReadOnlySpan<byte> b)
    {
        ulong n = 0;
        for (int i = 0; i < 8; i++) n = (n << 8) | b[i];

        if ((b[0] & 0x80) != 0) n ^= 0x8000000000000000UL; // เดิมเป็นค่าบวก
        else                    n = ~n;                      // เดิมเป็นค่าลบ

        return BitConverter.Int64BitsToDouble(unchecked((long)n));
    }

    private static DateTime? ReadDbfDateTime(ReadOnlySpan<byte> b)
    {
        if (b.Length < 8) return null;
        int julianDay = BinaryPrimitives.ReadInt32LittleEndian(b);
        int milliseconds = BinaryPrimitives.ReadInt32LittleEndian(b.Slice(4, 4));
        if (julianDay == 0) return null;

        // Julian day 2451545 = 2000-01-01 12:00 UTC; ใช้จุดอ้างอิงมาตรฐานของ dBASE
        var date = new DateTime(1, 1, 1).AddDays(julianDay - 1721426); // 1721426 = JD ของ 0001-01-01
        return date.AddMilliseconds(milliseconds);
    }
}
