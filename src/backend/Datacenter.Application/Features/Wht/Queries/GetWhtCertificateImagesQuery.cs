using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Wht.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Queries;

/// <summary>
/// เรนเดอร์หนังสือรับรองหัก ณ ที่จ่ายของรายการที่เลือกเป็นรูป PNG (data URL) สำหรับ preview ในเว็บ —
/// เลี่ยงปัญหา iframe PDF ขึ้นจอดำในบางเบราว์เซอร์. คืน data URL หน้าละ 1 รายการ.
/// </summary>
public record GetWhtCertificateImagesQuery(int ClientCompanyId, IReadOnlyList<int> EntryIds)
    : IRequest<IReadOnlyList<string>>, IRequireCompanyAccess;

public class GetWhtCertificateImagesQueryHandler(IApplicationDbContext db, IWhtCertificatePdfService pdf)
    : IRequestHandler<GetWhtCertificateImagesQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(GetWhtCertificateImagesQuery request, CancellationToken ct)
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
        return pdf.GenerateImages(models)
            .Select(png => "data:image/png;base64," + Convert.ToBase64String(png))
            .ToList();
    }
}
