namespace Datacenter.Domain.Exceptions;

public class DuplicateImportException : DomainException
{
    public DuplicateImportException(string batchRef)
        : base($"พบ Import batch ซ้ำ: {batchRef}") { }
}
