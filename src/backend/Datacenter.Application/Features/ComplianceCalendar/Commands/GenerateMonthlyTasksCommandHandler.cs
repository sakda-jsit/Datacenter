using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ComplianceCalendar.Services;
using MediatR;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public class GenerateMonthlyTasksCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<GenerateMonthlyTasksCommand, int>
{
    public async Task<int> Handle(GenerateMonthlyTasksCommand request, CancellationToken ct)
    {
        var toCreate = await ComplianceTaskGenerator.BuildMissingAsync(
            db, request.ClientCompanyId, request.Year, request.Month, ct);

        if (toCreate.Count == 0)
            return 0;

        db.ComplianceTasks.AddRange(toCreate);

        await audit.LogAsync("GenerateTasks", "ComplianceTask",
            $"{request.ClientCompanyId}:{request.Year}/{request.Month:D2}",
            afterValue: $"{toCreate.Count} tasks created",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return toCreate.Count;
    }
}
