namespace Datacenter.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"ไม่พบ {name} รหัส {key}") { }
}
