namespace Datacenter.Application.Features.Clients.DTOs;

// Name ใน ClientListDto = ชื่อทางการ (LegalName) สำหรับแสดงผล
public record ClientListDto(int Id, string Code, string Name, string TaxId, bool IsActive);

// Name = ชื่อจาก Express (อ้างอิง), LegalName = ชื่อทางการที่แก้ได้/ใช้ออกงบ
public record ClientDetailDto(
    int Id, string Code, string Name, string LegalName, string TaxId,
    string BranchCode, string? Address, int FiscalYearStartMonth, bool IsActive,
    string? SsoAccountNo, string? SsoBranchCode, string? Phone, string? PostalCode);

public record UpdateClientRequest(
    string LegalName, string TaxId, string BranchCode, string? Address, int FiscalYearStartMonth,
    string? SsoAccountNo, string? SsoBranchCode, string? Phone, string? PostalCode);
