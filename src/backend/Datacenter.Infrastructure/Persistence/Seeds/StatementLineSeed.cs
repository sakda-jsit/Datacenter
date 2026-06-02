using Datacenter.Domain.Entities;

namespace Datacenter.Infrastructure.Persistence.Seeds;

public static class StatementLineSeed
{
    /// <summary>
    /// บรรทัดมาตรฐานของงบการเงิน (master reference, ใช้ร่วมทุกบริษัท).
    /// Seed ตอน startup โดย DbInitializer ถ้าตารางว่าง.
    /// </summary>
    public static List<StatementLine> GetLines()
    {
        return new List<StatementLine>
        {
            // สินทรัพย์ (A)
            new() { RefCode = "A1",  LineName = "เงินสดและรายการเทียบเท่าเงินสด",                              Section = 'A', SortOrder = 11 },
            new() { RefCode = "A2",  LineName = "เงินลงทุนระยะสั้น",                                          Section = 'A', SortOrder = 12 },
            new() { RefCode = "A7",  LineName = "ลูกหนี้การค้าและลูกหนี้หมุนเวียนอื่น",                       Section = 'A', SortOrder = 13 },
            new() { RefCode = "A8",  LineName = "ลูกหนี้เงินให้กู้ยืมบุคคล/กิจการที่เกี่ยวข้อง",             Section = 'A', SortOrder = 14 },
            new() { RefCode = "A3",  LineName = "สินค้าคงเหลือ",                                              Section = 'A', SortOrder = 15 },
            new() { RefCode = "A4",  LineName = "สินทรัพย์หมุนเวียนอื่น",                                     Section = 'A', SortOrder = 16 },
            new() { RefCode = "TXR", LineName = "ภาษีเงินได้จ่ายล่วงหน้า (รอขอคืน)",                          Section = 'A', SortOrder = 17 },
            new() { RefCode = "A9",  LineName = "เงินลงทุนระยะยาว",                                           Section = 'A', SortOrder = 21 },
            new() { RefCode = "A5",  LineName = "ที่ดิน อาคารและอุปกรณ์",                                     Section = 'A', SortOrder = 22 },
            new() { RefCode = "A10", LineName = "สินทรัพย์ไม่มีตัวตน",                                        Section = 'A', SortOrder = 23 },
            new() { RefCode = "A6",  LineName = "สินทรัพย์ไม่หมุนเวียนอื่น",                                  Section = 'A', SortOrder = 24 },
            // หนี้สิน (L)
            new() { RefCode = "L1",  LineName = "เจ้าหนี้การค้าและเจ้าหนี้หมุนเวียนอื่น",                    Section = 'L', SortOrder = 31 },
            new() { RefCode = "L5",  LineName = "ส่วนของหนี้สินตามสัญญาเช่าที่ถึงกำหนดชำระภายในหนึ่งปี",    Section = 'L', SortOrder = 32 },
            new() { RefCode = "L3",  LineName = "เงินกู้ยืมระยะสั้น",                                         Section = 'L', SortOrder = 33 },
            new() { RefCode = "L2",  LineName = "หนี้สินหมุนเวียนอื่น",                                       Section = 'L', SortOrder = 34 },
            new() { RefCode = "TXP", LineName = "ภาษีเงินได้ค้างจ่าย",                                         Section = 'L', SortOrder = 35 },
            new() { RefCode = "L6",  LineName = "เงินกู้ยืมระยะยาว",                                          Section = 'L', SortOrder = 41 },
            new() { RefCode = "L4",  LineName = "หนี้สินตามสัญญาเช่า",                                        Section = 'L', SortOrder = 42 },
            // ทุน (E)
            new() { RefCode = "C1",  LineName = "ทุนที่ออกและชำระแล้ว",                                       Section = 'E', SortOrder = 51 },
            new() { RefCode = "RE",  LineName = "กำไร (ขาดทุน) สะสม",                                         Section = 'E', SortOrder = 52 },
            // รายได้ (I)
            new() { RefCode = "I1",  LineName = "รายได้จากการขาย",                                            Section = 'I', SortOrder = 61 },
            new() { RefCode = "I2",  LineName = "รายได้จากการให้บริการ",                                       Section = 'I', SortOrder = 62 },
            new() { RefCode = "I3",  LineName = "รายได้ดอกเบี้ย",                                             Section = 'I', SortOrder = 63 },
            new() { RefCode = "I4",  LineName = "รายได้อื่น",                                                  Section = 'I', SortOrder = 64 },
            // ค่าใช้จ่าย (X)
            new() { RefCode = "C",   LineName = "ต้นทุนขายหรือต้นทุนการให้บริการ",                            Section = 'X', SortOrder = 71 },
            new() { RefCode = "X1",  LineName = "ค่าใช้จ่ายในการขาย",                                         Section = 'X', SortOrder = 72 },
            new() { RefCode = "X2",  LineName = "ค่าใช้จ่ายในการบริหาร",                                      Section = 'X', SortOrder = 73 },
            new() { RefCode = "X3",  LineName = "ต้นทุนทางการเงิน",                                           Section = 'X', SortOrder = 74 },
            new() { RefCode = "X4",  LineName = "ภาษีเงินได้",                                                 Section = 'X', SortOrder = 75 },
        };
    }
}
