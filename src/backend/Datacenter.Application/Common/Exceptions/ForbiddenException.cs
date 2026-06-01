namespace Datacenter.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException() : base("ไม่มีสิทธิ์เข้าถึงข้อมูลนี้") { }
    public ForbiddenException(string message) : base(message) { }
}
