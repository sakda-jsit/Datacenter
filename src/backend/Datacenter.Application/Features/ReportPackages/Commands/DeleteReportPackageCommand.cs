using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ReportPackages.Commands;

/// <summary>ลบชุดรายงานงบ — ได้เฉพาะสถานะ Draft (Final/Locked ห้ามลบ)</summary>
public record DeleteReportPackageCommand(int ClientCompanyId, int Id) : IRequest<Unit>, IRequireCompanyAccess;

public class DeleteReportPackageCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<DeleteReportPackageCommand, Unit>
{
    public async Task<Unit> Handle(DeleteReportPackageCommand request, CancellationToken ct)
    {
        var pkg = await db.ReportPackages
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ReportPackage", request.Id);

        if (pkg.Status != ReportPackageStatus.Draft)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(
                "Status", "ลบได้เฉพาะชุดรายงานสถานะ Draft เท่านั้น") });

        db.ReportPackages.Remove(pkg);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("DeleteReportPackage", "ReportPackage", request.Id.ToString(),
            beforeValue: $"FY{pkg.FiscalYear} v{pkg.Version}", companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
