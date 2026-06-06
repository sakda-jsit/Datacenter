using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── อัปโหลดเอกสาร (รูปบัตร/หลักฐานแจ้ง ปกส. ฯลฯ) ───────────────────────────────
public record UploadEmployeeDocumentCommand(
    int ClientCompanyId, int EmployeeId, EmployeeDocType DocType,
    string FileName, string ContentType, byte[] Content,
    DateTime? EffectiveDate, string? Note)
    : IRequest<int>, IRequireCompanyAccess;

public class UploadEmployeeDocumentCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UploadEmployeeDocumentCommand, int>
{
    public async Task<int> Handle(UploadEmployeeDocumentCommand request, CancellationToken ct)
    {
        await EnsureEmployee(db, request.EmployeeId, request.ClientCompanyId, ct);
        if (request.Content is not { Length: > 0 })
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(
                "Content", "ไฟล์ว่าง") });

        var doc = new EmployeeDocument
        {
            EmployeeId = request.EmployeeId,
            DocType = request.DocType,
            FileName = request.FileName,
            ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType,
            Content = request.Content,
            EffectiveDate = request.EffectiveDate,
            Note = request.Note,
            CreatedBy = currentUser.Username,
        };
        db.EmployeeDocuments.Add(doc);
        await audit.LogAsync("UploadDocument", "EmployeeDocument",
            entityId: $"emp:{request.EmployeeId}", afterValue: $"{request.DocType} / {request.FileName}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return doc.Id;
    }

    internal static async Task<Employee> EnsureEmployee(IApplicationDbContext db, int empId, int companyId, CancellationToken ct)
        => await db.Employees.FirstOrDefaultAsync(e => e.Id == empId && e.ClientCompanyId == companyId, ct)
           ?? throw new NotFoundException("Employee", empId);
}

// ── ดาวน์โหลดเอกสาร (PDPA: audit ทุกการเข้าถึง) ────────────────────────────────
public record GetEmployeeDocumentContentQuery(int ClientCompanyId, int DocumentId)
    : IRequest<EmployeeDocumentContentDto>, IRequireCompanyAccess;

public class GetEmployeeDocumentContentQueryHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<GetEmployeeDocumentContentQuery, EmployeeDocumentContentDto>
{
    public async Task<EmployeeDocumentContentDto> Handle(GetEmployeeDocumentContentQuery request, CancellationToken ct)
    {
        var doc = await db.EmployeeDocuments
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId
                && d.Employee!.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("EmployeeDocument", request.DocumentId);

        // PDPA: บันทึกการเข้าถึงเอกสารส่วนบุคคล
        await audit.LogAsync("ViewDocument", "EmployeeDocument",
            entityId: $"emp:{doc.EmployeeId}:doc:{doc.Id}", afterValue: $"{doc.DocType} / {doc.FileName}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return new EmployeeDocumentContentDto(doc.FileName, doc.ContentType, doc.Content);
    }
}

// ── ลบเอกสาร ───────────────────────────────────────────────────────────────────
public record DeleteEmployeeDocumentCommand(int ClientCompanyId, int DocumentId)
    : IRequest<Unit>, IRequireCompanyAccess;

public class DeleteEmployeeDocumentCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<DeleteEmployeeDocumentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteEmployeeDocumentCommand request, CancellationToken ct)
    {
        var doc = await db.EmployeeDocuments
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId
                && d.Employee!.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("EmployeeDocument", request.DocumentId);

        db.EmployeeDocuments.Remove(doc);
        await audit.LogAsync("DeleteDocument", "EmployeeDocument",
            entityId: $"emp:{doc.EmployeeId}:doc:{doc.Id}", beforeValue: $"{doc.DocType} / {doc.FileName}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
