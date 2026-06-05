using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Queries;

/// <summary>สถานะลายเซ็นบริษัท: มีหรือไม่ + data URL สำหรับแสดงตัวอย่าง</summary>
public record WhtSignatureDto(bool HasSignature, string? DataUrl);

public record GetClientCompanySignatureQuery(int ClientCompanyId)
    : IRequest<WhtSignatureDto>, IRequireCompanyAccess;

public class GetClientCompanySignatureQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetClientCompanySignatureQuery, WhtSignatureDto>
{
    public async Task<WhtSignatureDto> Handle(GetClientCompanySignatureQuery request, CancellationToken ct)
    {
        var company = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        if (company.SignatureImage is not { Length: > 0 } img)
            return new WhtSignatureDto(false, null);

        return new WhtSignatureDto(true, "data:image/png;base64," + Convert.ToBase64String(img));
    }
}
