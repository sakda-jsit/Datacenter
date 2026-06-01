using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ComplianceCalendar.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public class GenerateMonthlyTasksCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<GenerateMonthlyTasksCommand, int>
{
    private static readonly ComplianceTaskType[] AllTypes = Enum.GetValues<ComplianceTaskType>();

    public async Task<int> Handle(GenerateMonthlyTasksCommand request, CancellationToken ct)
    {
        var existing = await db.ComplianceTasks
            .Where(t => t.ClientCompanyId == request.ClientCompanyId
                     && t.Year == request.Year
                     && t.Month == request.Month)
            .Select(t => t.TaskType)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        var toCreate = AllTypes
            .Where(type => !existingSet.Contains(type))
            .Select(type => new ComplianceTask
            {
                ClientCompanyId = request.ClientCompanyId,
                TaskType = type,
                Year = request.Year,
                Month = request.Month,
                DueDate = ComplianceDueDateCalculator.Calculate(type, request.Year, request.Month),
                Status = ComplianceTaskStatus.Pending,
            })
            .ToList();

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
