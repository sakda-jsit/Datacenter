using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ReportPackages.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ReportPackages.Commands;

/// <summary>สร้างชุดรายงานงบใหม่ (version ถัดไปของบริษัท+ปีนั้น) สถานะ Draft. ใช้สำหรับยื่นเพิ่มเติม = เปิด version ใหม่</summary>
public record CreateReportPackageCommand(int ClientCompanyId, int FiscalYear, string? Title = null, string? Note = null)
    : IRequest<ReportPackageDto>, IRequireCompanyAccess;

public class CreateReportPackageCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<CreateReportPackageCommand, ReportPackageDto>
{
    public async Task<ReportPackageDto> Handle(CreateReportPackageCommand request, CancellationToken ct)
    {
        var maxVersion = await db.ReportPackages
            .Where(p => p.ClientCompanyId == request.ClientCompanyId && p.FiscalYear == request.FiscalYear)
            .Select(p => (int?)p.Version)
            .MaxAsync(ct) ?? 0;

        var pkg = new ReportPackage
        {
            ClientCompanyId = request.ClientCompanyId,
            FiscalYear = request.FiscalYear,
            Version = maxVersion + 1,
            Status = ReportPackageStatus.Draft,
            Title = string.IsNullOrWhiteSpace(request.Title) ? $"งบการเงินปี {request.FiscalYear}" : request.Title.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedBy = currentUser.Username,
        };
        db.ReportPackages.Add(pkg);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("CreateReportPackage", "ReportPackage", pkg.Id.ToString(),
            afterValue: $"FY{pkg.FiscalYear} v{pkg.Version}", companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return ReportPackageMapper.ToDto(pkg);
    }
}
