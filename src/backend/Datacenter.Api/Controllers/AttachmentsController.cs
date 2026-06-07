using Datacenter.Application.Features.Attachments.Commands;
using Datacenter.Application.Features.Attachments.Queries;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/attachments")]
public class AttachmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/attachments?clientCompanyId=1&amp;fiscalYear=2025&amp;category=1&amp;moduleName=Bank&amp;recordId=5&amp;verificationStatus=0&amp;search=...</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int clientCompanyId, [FromQuery] int? fiscalYear, [FromQuery] AttachmentCategory? category,
        [FromQuery] string? moduleName, [FromQuery] int? recordId,
        [FromQuery] AttachmentVerificationStatus? verificationStatus, [FromQuery] string? search,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetAttachmentsQuery(
            clientCompanyId, fiscalYear, category, moduleName, recordId, verificationStatus, search), ct));

    /// <summary>GET /api/v1/attachments/completeness?clientCompanyId=1&amp;fiscalYear=2025 — checklist หลักฐานปิดงบ</summary>
    [HttpGet("completeness")]
    public async Task<IActionResult> GetCompleteness(
        [FromQuery] int clientCompanyId, [FromQuery] int fiscalYear, CancellationToken ct)
        => Ok(await mediator.Send(new GetEvidenceCompletenessQuery(clientCompanyId, fiscalYear), ct));

    /// <summary>GET /api/v1/attachments/{id}/download?clientCompanyId=1 — ดาวน์โหลดเนื้อไฟล์ (audit)</summary>
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        var d = await mediator.Send(new GetAttachmentContentQuery(clientCompanyId, id), ct);
        return File(d.Content, d.ContentType, d.FileName);
    }

    /// <summary>POST /api/v1/attachments?clientCompanyId=1 (multipart: file + fields)</summary>
    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromQuery] int clientCompanyId,
        [FromForm] AttachmentCategory category,
        [FromForm] string title,
        IFormFile file,
        [FromForm] int? fiscalYear,
        [FromForm] string? moduleName,
        [FromForm] int? recordId,
        [FromForm] string? recordRef,
        [FromForm] DateTime? documentDate,
        [FromForm] string? note,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "กรุณาเลือกไฟล์" });
        if (file.Length > 15 * 1024 * 1024)
            return BadRequest(new { detail = "ไฟล์ใหญ่เกิน 15 MB" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var id = await mediator.Send(new UploadAttachmentCommand(
            clientCompanyId, category, fiscalYear, moduleName, recordId, recordRef,
            title, file.FileName, file.ContentType ?? "application/octet-stream", ms.ToArray(),
            documentDate, note), ct);
        return Ok(new { id });
    }

    /// <summary>PUT /api/v1/attachments/{id}?clientCompanyId=1 — แก้ไข metadata</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, [FromQuery] int clientCompanyId, [FromBody] UpdateAttachmentBody body, CancellationToken ct)
    {
        await mediator.Send(new UpdateAttachmentMetadataCommand(
            clientCompanyId, id, body.Category, body.FiscalYear, body.RecordRef,
            body.Title, body.DocumentDate, body.Note), ct);
        return NoContent();
    }

    /// <summary>PUT /api/v1/attachments/{id}/verification?clientCompanyId=1 — ตั้งสถานะตรวจสอบ</summary>
    [HttpPut("{id:int}/verification")]
    public async Task<IActionResult> SetVerification(
        int id, [FromQuery] int clientCompanyId, [FromBody] SetVerificationBody body, CancellationToken ct)
    {
        await mediator.Send(new SetAttachmentVerificationCommand(clientCompanyId, id, body.Status, body.Note), ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/attachments/{id}?clientCompanyId=1</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteAttachmentCommand(clientCompanyId, id), ct);
        return NoContent();
    }

    public record UpdateAttachmentBody(
        AttachmentCategory Category, int? FiscalYear, string? RecordRef, string Title,
        DateTime? DocumentDate, string? Note);

    public record SetVerificationBody(AttachmentVerificationStatus Status, string? Note);
}
