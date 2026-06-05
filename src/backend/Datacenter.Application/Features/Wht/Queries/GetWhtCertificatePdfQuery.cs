using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Wht.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Queries;

/// <summary>สร้าง PDF หนังสือรับรองหัก ณ ที่จ่ายของรายการที่เลือก (preview, req 2) — รวมหลายใบเป็นหลายหน้า</summary>
public record GetWhtCertificatePdfQuery(int ClientCompanyId, IReadOnlyList<int> EntryIds)
    : IRequest<byte[]>, IRequireCompanyAccess;

public class GetWhtCertificatePdfQueryHandler(IApplicationDbContext db, IWhtCertificatePdfService pdf)
    : IRequestHandler<GetWhtCertificatePdfQuery, byte[]>
{
    public async Task<byte[]> Handle(GetWhtCertificatePdfQuery request, CancellationToken ct)
    {
        var payer = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var ids = request.EntryIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(
                "EntryIds", "กรุณาเลือกรายการอย่างน้อย 1 รายการ") });

        var entries = await db.WhtEntries
            .AsNoTracking()
            .Where(w => w.ClientCompanyId == request.ClientCompanyId && ids.Contains(w.Id))
            .OrderBy(w => w.TaxPeriod).ThenBy(w => w.DocumentNo)
            .ToListAsync(ct);

        if (entries.Count == 0)
            throw new NotFoundException("WhtEntry", string.Join(",", ids));

        var models = entries.Select(e => WhtCertificateBuilder.Build(e, payer)).ToList();
        return pdf.Generate(models);
    }
}
