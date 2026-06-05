using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Commands;

/// <summary>อัปโหลด/ลบรูปลายเซ็นผู้มีอำนาจของบริษัท (ใช้แนบในหนังสือรับรองหัก ณ ที่จ่าย). Image=null = ลบ</summary>
public record UpdateClientCompanySignatureCommand(int ClientCompanyId, byte[]? Image)
    : IRequest<Unit>, IRequireCompanyAccess;

public class UpdateClientCompanySignatureCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, ISignatureImageProcessor imageProcessor)
    : IRequestHandler<UpdateClientCompanySignatureCommand, Unit>
{
    public async Task<Unit> Handle(UpdateClientCompanySignatureCommand request, CancellationToken ct)
    {
        var company = await db.ClientCompanies
            .FirstOrDefaultAsync(c => c.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        company.SignatureImage = request.Image is { Length: > 0 }
            ? imageProcessor.TrimWhitespace(request.Image)
            : null;
        company.ModifiedBy = currentUser.Username;
        company.ModifiedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
