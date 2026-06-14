using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.AuditRegistry;

// ทะเบียนผู้ทำบัญชี = master ของสำนักงาน (ใช้ซ้ำหลายบริษัท) — ค่ากลาง ไม่ผูก IRequireCompanyAccess.

public record BookkeeperDto(int Id, string Name, string? TaxId, bool IsActive);
public record BookkeeperInput(string Name, string? TaxId, bool IsActive);

public record GetBookkeepersQuery : IRequest<IReadOnlyList<BookkeeperDto>>;

public class GetBookkeepersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBookkeepersQuery, IReadOnlyList<BookkeeperDto>>
{
    public async Task<IReadOnlyList<BookkeeperDto>> Handle(GetBookkeepersQuery req, CancellationToken ct)
        => (await db.Bookkeepers.AsNoTracking().OrderByDescending(x => x.IsActive).ThenBy(x => x.Name).ToListAsync(ct))
            .Select(b => new BookkeeperDto(b.Id, b.Name, b.TaxId, b.IsActive)).ToList();
}

public record UpsertBookkeeperCommand(int? Id, BookkeeperInput Data) : IRequest<int>;

public class UpsertBookkeeperCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAuditService audit)
    : IRequestHandler<UpsertBookkeeperCommand, int>
{
    public async Task<int> Handle(UpsertBookkeeperCommand req, CancellationToken ct)
    {
        var d = req.Data;
        Bookkeeper b;
        bool isNew = req.Id is null;
        if (req.Id is { } id)
        {
            b = await db.Bookkeepers.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException("Bookkeeper", id);
            b.ModifiedBy = user.Username; b.ModifiedAt = DateTime.UtcNow;
        }
        else { b = new Bookkeeper { CreatedBy = user.Username }; db.Bookkeepers.Add(b); }

        b.Name = (d.Name ?? "").Trim();
        b.TaxId = AuditorMap_Digits(d.TaxId);
        b.IsActive = d.IsActive;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(isNew ? "Create" : "Update", "Bookkeeper", b.Id.ToString(),
            afterValue: $"{b.Name} / {b.TaxId}", cancellationToken: ct);
        return b.Id;
    }

    static string? AuditorMap_Digits(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : new string(s.Where(char.IsDigit).ToArray());
}

public record DeleteBookkeeperCommand(int Id) : IRequest;

public class DeleteBookkeeperCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAuditService audit)
    : IRequestHandler<DeleteBookkeeperCommand>
{
    public async Task Handle(DeleteBookkeeperCommand req, CancellationToken ct)
    {
        var b = await db.Bookkeepers.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Bookkeeper", req.Id);
        b.IsActive = false;
        b.ModifiedBy = user.Username; b.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("Delete", "Bookkeeper", b.Id.ToString(), afterValue: b.Name, cancellationToken: ct);
    }
}

public class BookkeeperInputValidator : AbstractValidator<BookkeeperInput>
{
    public BookkeeperInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("ต้องระบุชื่อผู้ทำบัญชี").MaximumLength(200);
        RuleFor(x => x.TaxId)
            .Must(v => string.IsNullOrWhiteSpace(v) || v.Count(char.IsDigit) == 13)
            .WithMessage("เลขผู้เสียภาษีผู้ทำบัญชีต้องมี 13 หลัก");
    }
}

public class UpsertBookkeeperCommandValidator : AbstractValidator<UpsertBookkeeperCommand>
{
    public UpsertBookkeeperCommandValidator()
        => RuleFor(x => x.Data).NotNull().SetValidator(new BookkeeperInputValidator());
}
