using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Wht.DTOs;
using Datacenter.Application.Features.Wht.Services;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Commands;

/// <summary>
/// ส่งหนังสือรับรองหัก ณ ที่จ่ายทางอีเมล (req 3,5,6) — จัดกลุ่มตามผู้ถูกหัก (1 อีเมล/ผู้ถูกหัก แนบ PDF ทุกใบ).
/// อัปเดตสถานะส่งเมลของแต่ละ entry (ส่งแล้ว+วันเวลา+ผู้ส่ง / ส่งไม่สำเร็จ+error).
/// </summary>
public record SendWhtCertificatesCommand(int ClientCompanyId, IReadOnlyList<int> EntryIds)
    : IRequest<IReadOnlyList<WhtSendResultDto>>, IRequireCompanyAccess;

public class SendWhtCertificatesCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IWhtCertificatePdfService pdf,
    IEmailSender email,
    IAuditService audit)
    : IRequestHandler<SendWhtCertificatesCommand, IReadOnlyList<WhtSendResultDto>>
{
    public async Task<IReadOnlyList<WhtSendResultDto>> Handle(SendWhtCertificatesCommand request, CancellationToken ct)
    {
        var payer = await db.ClientCompanies
            .FirstOrDefaultAsync(c => c.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var ids = request.EntryIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(
                "EntryIds", "กรุณาเลือกรายการอย่างน้อย 1 รายการ") });

        var entries = await db.WhtEntries
            .Where(w => w.ClientCompanyId == request.ClientCompanyId && ids.Contains(w.Id))
            .ToListAsync(ct);
        if (entries.Count == 0)
            throw new NotFoundException("WhtEntry", string.Join(",", ids));

        var emailByTax = await db.WhtPayees
            .Where(p => p.ClientCompanyId == request.ClientCompanyId)
            .ToDictionaryAsync(p => p.TaxId, p => p.Email, ct);

        var results = new List<WhtSendResultDto>();
        var now = DateTime.UtcNow;
        var sender = currentUser.Username;

        // จัดกลุ่มตามผู้ถูกหัก (TaxId)
        foreach (var group in entries.GroupBy(e => e.PayeeTaxId ?? ""))
        {
            var taxId = group.Key;
            var groupEntries = group.ToList();
            var payeeName = groupEntries[0].PayeeName;
            emailByTax.TryGetValue(taxId, out var recipient);

            string? error = null;
            if (string.IsNullOrWhiteSpace(recipient))
            {
                error = "ไม่พบอีเมลของผู้ถูกหัก — กรุณากำหนดอีเมลก่อน";
            }
            else
            {
                try
                {
                    var models = groupEntries.Select(e => WhtCertificateBuilder.Build(e, payer)).ToList();
                    var pdfBytes = pdf.Generate(models);
                    var attachments = new[]
                    {
                        new EmailAttachment($"wht-{taxId}-{now:yyyyMMddHHmmss}.pdf", pdfBytes),
                    };
                    var subject = $"หนังสือรับรองการหักภาษี ณ ที่จ่าย จาก {(string.IsNullOrWhiteSpace(payer.LegalName) ? payer.Name : payer.LegalName)}";
                    var body = $"<p>เรียน {payeeName}</p><p>แนบหนังสือรับรองการหักภาษี ณ ที่จ่าย จำนวน {groupEntries.Count} ฉบับ</p>";
                    await email.SendAsync(new EmailMessage(recipient!, subject, body, attachments), ct);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }

            // อัปเดตสถานะทุก entry ในกลุ่ม
            foreach (var e in groupEntries)
            {
                if (error is null)
                {
                    e.EmailStatus = WhtEmailStatus.Sent;
                    e.EmailRecipient = recipient;
                    e.EmailSentAt = now;
                    e.EmailSentBy = sender;
                    e.EmailError = null;
                }
                else
                {
                    e.EmailStatus = WhtEmailStatus.Failed;
                    e.EmailError = error.Length > 500 ? error[..500] : error;
                    e.EmailSentBy = sender;
                    e.EmailSentAt = now;
                }
            }

            results.Add(new WhtSendResultDto(taxId, payeeName, recipient, error is null, groupEntries.Count, error));
        }

        await db.SaveChangesAsync(ct);

        var sent = results.Count(r => r.Success);
        await audit.LogAsync(
            action: "SendWhtCertificates",
            entityName: "WhtEntry",
            entityId: string.Join(",", ids),
            afterValue: $"sent {sent}/{results.Count} groups, entries {entries.Count}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return results;
    }
}
