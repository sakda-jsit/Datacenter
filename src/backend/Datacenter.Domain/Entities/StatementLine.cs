namespace Datacenter.Domain.Entities;

public class StatementLine
{
    public string RefCode { get; set; } = string.Empty;
    public string LineName { get; set; } = string.Empty;
    public char Section { get; set; }
    public int SortOrder { get; set; }
}
