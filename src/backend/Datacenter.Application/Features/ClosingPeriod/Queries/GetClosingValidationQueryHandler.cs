using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using Datacenter.Application.Features.ClosingPeriod.Services;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ClosingPeriod.Queries;

public class GetClosingValidationQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetClosingValidationQuery, ClosingValidationDto>
{
    public async Task<ClosingValidationDto> Handle(GetClosingValidationQuery request, CancellationToken ct)
    {
        var exists = await db.ClientCompanies
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.ClientCompanyId, ct);
        if (!exists)
            throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var current = await db.ClosingPeriods
            .AsNoTracking()
            .Where(p => p.ClientCompanyId == request.ClientCompanyId
                     && p.Year == request.Year
                     && p.Month == request.Month)
            .Select(p => (PeriodStatus?)p.Status)
            .FirstOrDefaultAsync(ct) ?? PeriodStatus.Open;

        var items = await ClosingValidationService.ValidateAsync(
            db, request.ClientCompanyId, request.Year, request.Month, ct);

        return new ClosingValidationDto(
            request.ClientCompanyId,
            request.Year,
            request.Month,
            current,
            ClosingValidationService.CanClose(items),
            items);
    }
}
