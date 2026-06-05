using Datacenter.Application.Features.Wht.Commands;
using Datacenter.Application.Features.Wht.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/wht")]
public class WhtController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/wht/report?clientCompanyId=1&amp;year=2025 — รายงาน ภ.ง.ด.3/53 รายเดือน</summary>
    [HttpGet("report")]
    public async Task<IActionResult> GetReport([FromQuery] GetWhtReportQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/wht/years?clientCompanyId=1 — ปีภาษีที่มีข้อมูล</summary>
    [HttpGet("years")]
    public async Task<IActionResult> GetYears([FromQuery] GetWhtYearsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/wht?clientCompanyId=1&amp;year=2025&amp;month=0&amp;formType= — รายละเอียดภาษีหัก ณ ที่จ่าย</summary>
    [HttpGet]
    public async Task<IActionResult> GetEntries([FromQuery] GetWhtEntriesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/wht/certificate?clientCompanyId=1&amp;entryIds=1&amp;entryIds=2 — PDF หนังสือรับรอง (preview)</summary>
    [HttpGet("certificate")]
    public async Task<IActionResult> GetCertificate([FromQuery] int clientCompanyId, [FromQuery] int[] entryIds, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetWhtCertificatePdfQuery(clientCompanyId, entryIds ?? []), ct);
        return File(bytes, "application/pdf", "wht-certificate.pdf");
    }

    /// <summary>GET /api/v1/wht/certificate/images?clientCompanyId=1&amp;entryIds=1&amp;entryIds=2 — รูป PNG (data URL) สำหรับ preview</summary>
    [HttpGet("certificate/images")]
    public async Task<IActionResult> GetCertificateImages([FromQuery] int clientCompanyId, [FromQuery] int[] entryIds, CancellationToken ct)
        => Ok(await mediator.Send(new GetWhtCertificateImagesQuery(clientCompanyId, entryIds ?? []), ct));

    /// <summary>PUT /api/v1/wht/payee-email — กำหนด/แก้ไขอีเมลผู้ถูกหัก</summary>
    [HttpPut("payee-email")]
    public async Task<IActionResult> UpdatePayeeEmail([FromBody] UpdateWhtPayeeEmailCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/wht/send — ส่งหนังสือรับรองทางอีเมล (จัดกลุ่มตามผู้ถูกหัก)</summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendWhtCertificatesCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>GET /api/v1/wht/signature?clientCompanyId=1 — สถานะ/ตัวอย่างลายเซ็นผู้มีอำนาจ</summary>
    [HttpGet("signature")]
    public async Task<IActionResult> GetSignature([FromQuery] int clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetClientCompanySignatureQuery(clientCompanyId), ct));

    /// <summary>POST /api/v1/wht/signature?clientCompanyId=1 — อัปโหลดรูปลายเซ็น (multipart: file)</summary>
    [HttpPost("signature")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadSignature([FromQuery] int clientCompanyId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "กรุณาเลือกไฟล์รูปลายเซ็น" });
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { detail = "ไฟล์ใหญ่เกิน 2 MB" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        await mediator.Send(new UpdateClientCompanySignatureCommand(clientCompanyId, ms.ToArray()), ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/wht/signature?clientCompanyId=1 — ลบรูปลายเซ็น</summary>
    [HttpDelete("signature")]
    public async Task<IActionResult> DeleteSignature([FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new UpdateClientCompanySignatureCommand(clientCompanyId, null), ct);
        return NoContent();
    }
}
