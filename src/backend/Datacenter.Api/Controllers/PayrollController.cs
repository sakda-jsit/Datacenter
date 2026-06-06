using Datacenter.Application.Features.Payroll.Commands;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Queries;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/payroll")]
public class PayrollController(IMediator mediator) : ControllerBase
{
    // ── พนักงาน ──────────────────────────────────────────────────────────────
    /// <summary>GET /api/v1/payroll/employees?clientCompanyId=1&amp;includeResigned=true</summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees([FromQuery] int clientCompanyId, [FromQuery] bool includeResigned = true, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetEmployeesQuery(clientCompanyId, includeResigned), ct));

    /// <summary>GET /api/v1/payroll/employees/{id}?clientCompanyId=1 — รายละเอียด + เอกสาร + การแจ้ง ปกส.</summary>
    [HttpGet("employees/{id:int}")]
    public async Task<IActionResult> GetEmployee(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetEmployeeQuery(id, clientCompanyId), ct));

    /// <summary>POST /api/v1/payroll/employees?clientCompanyId=1 (body: EmployeeInput)</summary>
    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromQuery] int clientCompanyId, [FromBody] EmployeeInput data, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new CreateEmployeeCommand(clientCompanyId, data), ct) });

    /// <summary>PUT /api/v1/payroll/employees/{id}?clientCompanyId=1 (body: EmployeeInput)</summary>
    [HttpPut("employees/{id:int}")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromQuery] int clientCompanyId, [FromBody] EmployeeInput data, CancellationToken ct)
    {
        await mediator.Send(new UpdateEmployeeCommand(id, clientCompanyId, data), ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/payroll/employees/{id}?clientCompanyId=1</summary>
    [HttpDelete("employees/{id:int}")]
    public async Task<IActionResult> DeleteEmployee(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteEmployeeCommand(id, clientCompanyId), ct);
        return NoContent();
    }

    // ── เอกสาร/หลักฐาน (PDPA) ─────────────────────────────────────────────────
    /// <summary>POST /api/v1/payroll/employees/{id}/documents?clientCompanyId=1&amp;docType=1 (multipart: file)</summary>
    [HttpPost("employees/{id:int}/documents")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(
        int id, [FromQuery] int clientCompanyId, [FromQuery] EmployeeDocType docType,
        IFormFile file, [FromQuery] DateTime? effectiveDate, [FromQuery] string? note, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "กรุณาเลือกไฟล์" });
        if (file.Length > 8 * 1024 * 1024)
            return BadRequest(new { detail = "ไฟล์ใหญ่เกิน 8 MB" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var docId = await mediator.Send(new UploadEmployeeDocumentCommand(
            clientCompanyId, id, docType, file.FileName, file.ContentType ?? "application/octet-stream",
            ms.ToArray(), effectiveDate, note), ct);
        return Ok(new { id = docId });
    }

    /// <summary>GET /api/v1/payroll/documents/{docId}?clientCompanyId=1 — ดาวน์โหลด (audit PDPA)</summary>
    [HttpGet("documents/{docId:int}")]
    public async Task<IActionResult> GetDocument(int docId, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        var d = await mediator.Send(new GetEmployeeDocumentContentQuery(clientCompanyId, docId), ct);
        return File(d.Content, d.ContentType, d.FileName);
    }

    /// <summary>DELETE /api/v1/payroll/documents/{docId}?clientCompanyId=1</summary>
    [HttpDelete("documents/{docId:int}")]
    public async Task<IActionResult> DeleteDocument(int docId, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteEmployeeDocumentCommand(clientCompanyId, docId), ct);
        return NoContent();
    }

    // ── การแจ้งเข้า-ออก ปกส. ──────────────────────────────────────────────────
    /// <summary>POST /api/v1/payroll/sso-enrollments?clientCompanyId=1 (body: {employeeId,type,eventDate,note})</summary>
    [HttpPost("sso-enrollments")]
    public async Task<IActionResult> CreateEnrollment([FromQuery] int clientCompanyId, [FromBody] CreateEnrollmentBody body, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new CreateSsoEnrollmentCommand(
            clientCompanyId, body.EmployeeId, body.Type, body.EventDate, body.Note), ct) });

    /// <summary>PUT /api/v1/payroll/sso-enrollments/{id}?clientCompanyId=1 (แจ้งแล้ว + แนบหลักฐาน)</summary>
    [HttpPut("sso-enrollments/{id:int}")]
    public async Task<IActionResult> UpdateEnrollment(int id, [FromQuery] int clientCompanyId, [FromBody] UpdateEnrollmentBody body, CancellationToken ct)
    {
        await mediator.Send(new UpdateSsoEnrollmentCommand(
            clientCompanyId, id, body.SubmittedDate, body.Status, body.ProofDocumentId, body.Note), ct);
        return NoContent();
    }

    public record CreateEnrollmentBody(int EmployeeId, SsoEnrollmentType Type, DateTime EventDate, string? Note);
    public record UpdateEnrollmentBody(DateTime? SubmittedDate, SsoEnrollmentStatus Status, int? ProofDocumentId, string? Note);

    // ── อัตราเงินสมทบ ปกส./กองทุนทดแทน (effective-dated) ──────────────────────
    /// <summary>GET /api/v1/payroll/config?clientCompanyId=1 — ค่ากลาง + ของบริษัท</summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfigs([FromQuery] int clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetPayrollConfigsQuery(clientCompanyId), ct));

    /// <summary>GET /api/v1/payroll/config/effective?clientCompanyId=1&amp;asOf=2025-06-01 — อัตราที่มีผล</summary>
    [HttpGet("config/effective")]
    public async Task<IActionResult> GetEffectiveConfig([FromQuery] int clientCompanyId, [FromQuery] DateTime asOf, CancellationToken ct)
        => Ok(await mediator.Send(new GetEffectivePayrollConfigQuery(clientCompanyId, asOf), ct));

    /// <summary>POST /api/v1/payroll/config?clientCompanyId=1 (body: PayrollRateConfigInput) — เพิ่มอัตราของบริษัท</summary>
    [HttpPost("config")]
    public async Task<IActionResult> CreateConfig([FromQuery] int clientCompanyId, [FromBody] PayrollRateConfigInput data, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new UpsertPayrollConfigCommand(clientCompanyId, null, data), ct) });

    /// <summary>PUT /api/v1/payroll/config/{id}?clientCompanyId=1 (body: PayrollRateConfigInput)</summary>
    [HttpPut("config/{id:int}")]
    public async Task<IActionResult> UpdateConfig(int id, [FromQuery] int clientCompanyId, [FromBody] PayrollRateConfigInput data, CancellationToken ct)
    {
        await mediator.Send(new UpsertPayrollConfigCommand(clientCompanyId, id, data), ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/payroll/config/{id}?clientCompanyId=1</summary>
    [HttpDelete("config/{id:int}")]
    public async Task<IActionResult> DeleteConfig(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeletePayrollConfigCommand(clientCompanyId, id), ct);
        return NoContent();
    }
}
