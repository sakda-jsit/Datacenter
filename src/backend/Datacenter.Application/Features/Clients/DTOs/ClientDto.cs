namespace Datacenter.Application.Features.Clients.DTOs;

public record ClientListDto(int Id, string Code, string Name, string TaxId, bool IsActive);

public record ClientDetailDto(
    int Id, string Code, string Name, string TaxId,
    string BranchCode, string? Address, int FiscalYearStartMonth, bool IsActive);

public record UpdateClientRequest(
    string Name, string TaxId, string BranchCode, string? Address, int FiscalYearStartMonth);
