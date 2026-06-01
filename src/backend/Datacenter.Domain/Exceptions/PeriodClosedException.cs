namespace Datacenter.Domain.Exceptions;

public class PeriodClosedException : DomainException
{
    public PeriodClosedException(string period)
        : base($"รอบบัญชี {period} ถูกปิดแล้ว ไม่สามารถแก้ไขข้อมูลได้") { }
}
