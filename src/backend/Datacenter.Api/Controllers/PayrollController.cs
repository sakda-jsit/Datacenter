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

    // ── งวดเงินเดือนรายเดือน ───────────────────────────────────────────────────
    /// <summary>GET /api/v1/payroll/runs?clientCompanyId=1&amp;year=2025</summary>
    [HttpGet("runs")]
    public async Task<IActionResult> GetRuns([FromQuery] int clientCompanyId, [FromQuery] int? year, CancellationToken ct)
        => Ok(await mediator.Send(new GetPayrollRunsQuery(clientCompanyId, year), ct));

    /// <summary>GET /api/v1/payroll/runs/{id}?clientCompanyId=1 — รายละเอียด + ค่าคำนวณเทียบ</summary>
    [HttpGet("runs/{id:int}")]
    public async Task<IActionResult> GetRun(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetPayrollRunQuery(clientCompanyId, id), ct));

    /// <summary>GET /api/v1/payroll/pnd1k?clientCompanyId=1&amp;year=2025 — ภ.ง.ด.1ก (สรุปทั้งปี)</summary>
    [HttpGet("pnd1k")]
    public async Task<IActionResult> GetPnd1k([FromQuery] int clientCompanyId, [FromQuery] int year, CancellationToken ct)
        => Ok(await mediator.Send(new GetPnd1kQuery(clientCompanyId, year), ct));

    /// <summary>GET /api/v1/payroll/pnd1k/excel?clientCompanyId=1&amp;year=2025</summary>
    [HttpGet("pnd1k/excel")]
    public async Task<IActionResult> GetPnd1kExcel([FromQuery] int clientCompanyId, [FromQuery] int year, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetPnd1kExcelQuery(clientCompanyId, year), ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"pnd1k-{year}.xlsx");
    }

    /// <summary>GET /api/v1/payroll/pnd1k/pdf?clientCompanyId=1&amp;year=2025</summary>
    [HttpGet("pnd1k/pdf")]
    public async Task<IActionResult> GetPnd1kPdf([FromQuery] int clientCompanyId, [FromQuery] int year, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetPnd1kPdfQuery(clientCompanyId, year), ct);
        return File(bytes, "application/pdf", $"pnd1k-{year}.pdf");
    }

    /// <summary>GET /api/v1/payroll/50tawi/pdf?clientCompanyId=1&amp;year=2025&amp;employeeIds=1&amp;employeeIds=2 — 50 ทวิ เงินเดือน (employeeIds ว่าง = ทุกคน)</summary>
    [HttpGet("50tawi/pdf")]
    public async Task<IActionResult> Get50TawiPdf([FromQuery] int clientCompanyId, [FromQuery] int year, [FromQuery] int[]? employeeIds, CancellationToken ct)
    {
        var bytes = await mediator.Send(new Get50TawiSalaryPdfQuery(clientCompanyId, year, employeeIds), ct);
        return File(bytes, "application/pdf", $"50tawi-salary-{year}.pdf");
    }

    /// <summary>GET /api/v1/payroll/50tawi/images?clientCompanyId=1&amp;year=2025&amp;employeeIds=1 — รูป PNG (data URL) สำหรับ preview</summary>
    [HttpGet("50tawi/images")]
    public async Task<IActionResult> Get50TawiImages([FromQuery] int clientCompanyId, [FromQuery] int year, [FromQuery] int[]? employeeIds, CancellationToken ct)
        => Ok(await mediator.Send(new Get50TawiSalaryImagesQuery(clientCompanyId, year, employeeIds), ct));

    /// <summary>GET /api/v1/payroll/kt20?clientCompanyId=1&amp;year=2025 — กท.20ก (แสดงเงินค่าจ้างประจำปี กองทุนเงินทดแทน)</summary>
    [HttpGet("kt20")]
    public async Task<IActionResult> GetKt20([FromQuery] int clientCompanyId, [FromQuery] int year, CancellationToken ct)
        => Ok(await mediator.Send(new GetKt20Query(clientCompanyId, year), ct));

    /// <summary>GET /api/v1/payroll/kt20/excel?clientCompanyId=1&amp;year=2025</summary>
    [HttpGet("kt20/excel")]
    public async Task<IActionResult> GetKt20Excel([FromQuery] int clientCompanyId, [FromQuery] int year, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetKt20ExcelQuery(clientCompanyId, year), ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"kt20-{year}.xlsx");
    }

    /// <summary>GET /api/v1/payroll/kt20/pdf?clientCompanyId=1&amp;year=2025</summary>
    [HttpGet("kt20/pdf")]
    public async Task<IActionResult> GetKt20Pdf([FromQuery] int clientCompanyId, [FromQuery] int year, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetKt20PdfQuery(clientCompanyId, year), ct);
        return File(bytes, "application/pdf", $"kt20-{year}.pdf");
    }

    /// <summary>GET /api/v1/payroll/kt20/images?clientCompanyId=1&amp;year=2025 — รูป PNG (data URL) สำหรับ preview</summary>
    [HttpGet("kt20/images")]
    public async Task<IActionResult> GetKt20Images([FromQuery] int clientCompanyId, [FromQuery] int year, CancellationToken ct)
        => Ok(await mediator.Send(new GetKt20ImagesQuery(clientCompanyId, year), ct));

    /// <summary>GET /api/v1/payroll/dashboard?clientCompanyId=1&amp;year=2025 — แดชบอร์ด/checklist + กระทบยอด 3 ทาง</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int clientCompanyId, [FromQuery] int year, CancellationToken ct)
        => Ok(await mediator.Send(new GetPayrollDashboardQuery(clientCompanyId, year), ct));

    // ── สถานะยื่น สปส.1-10 + ใบเสร็จ ──────────────────────────────────────────
    /// <summary>PUT /api/v1/payroll/runs/{runId}/sso-filing/status?clientCompanyId=1 — บันทึกสถานะยื่น/ใบเสร็จ</summary>
    [HttpPut("runs/{runId:int}/sso-filing/status")]
    public async Task<IActionResult> UpsertSsoFilingStatus(
        int runId, [FromQuery] int clientCompanyId, [FromBody] SsoFilingStatusInput body, CancellationToken ct)
    {
        var id = await mediator.Send(new UpsertSsoFilingStatusCommand(
            clientCompanyId, runId, body.SubmittedDate, body.ReceiptDate, body.ReceiptAmount,
            body.ReceiptNo, body.Note), ct);
        return Ok(new { id });
    }

    /// <summary>POST /api/v1/payroll/runs/{runId}/sso-filing/document?clientCompanyId=1&amp;kind=form|receipt (multipart: file)</summary>
    [HttpPost("runs/{runId:int}/sso-filing/document")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadSsoFilingDocument(
        int runId, [FromQuery] int clientCompanyId, [FromQuery] string kind, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "กรุณาเลือกไฟล์" });
        if (file.Length > 8 * 1024 * 1024)
            return BadRequest(new { detail = "ไฟล์ใหญ่เกิน 8 MB" });
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var id = await mediator.Send(new UploadSsoFilingDocumentCommand(
            clientCompanyId, runId, kind, file.FileName, file.ContentType ?? "application/octet-stream", ms.ToArray()), ct);
        return Ok(new { id });
    }

    /// <summary>GET /api/v1/payroll/runs/{runId}/sso-filing/document?clientCompanyId=1&amp;kind=form|receipt — ดาวน์โหลดไฟล์แนบ</summary>
    [HttpGet("runs/{runId:int}/sso-filing/document")]
    public async Task<IActionResult> GetSsoFilingDocument(
        int runId, [FromQuery] int clientCompanyId, [FromQuery] string kind, CancellationToken ct)
    {
        var d = await mediator.Send(new GetSsoFilingDocumentQuery(clientCompanyId, runId, kind), ct);
        return File(d.Content, d.ContentType, d.FileName);
    }

    /// <summary>GET /api/v1/payroll/year-summary?clientCompanyId=1&amp;year=2025 — สรุปรายได้ทั้งปี (แถว=เดือน)</summary>
    [HttpGet("year-summary")]
    public async Task<IActionResult> GetYearSummary([FromQuery] int clientCompanyId, [FromQuery] int year, CancellationToken ct)
        => Ok(await mediator.Send(new GetPayrollYearSummaryQuery(clientCompanyId, year), ct));

    /// <summary>POST /api/v1/payroll/runs?clientCompanyId=1 (body: {year,month}) — สร้างงวด + prefill พนักงาน</summary>
    [HttpPost("runs")]
    public async Task<IActionResult> CreateRun([FromQuery] int clientCompanyId, [FromBody] CreateRunBody body, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new CreatePayrollRunCommand(clientCompanyId, body.Year, body.Month), ct) });

    /// <summary>PUT /api/v1/payroll/runs/{id}/items?clientCompanyId=1 (body: PayrollItemInput[]) — บันทึก grid</summary>
    [HttpPut("runs/{id:int}/items")]
    public async Task<IActionResult> SaveItems(int id, [FromQuery] int clientCompanyId, [FromBody] List<PayrollItemInput> items, CancellationToken ct)
    {
        await mediator.Send(new SavePayrollItemsCommand(clientCompanyId, id, items ?? []), ct);
        return NoContent();
    }

    /// <summary>PUT /api/v1/payroll/runs/{id}/status?clientCompanyId=1 (body: {status})</summary>
    [HttpPut("runs/{id:int}/status")]
    public async Task<IActionResult> SetRunStatus(int id, [FromQuery] int clientCompanyId, [FromBody] SetStatusBody body, CancellationToken ct)
    {
        await mediator.Send(new SetPayrollRunStatusCommand(clientCompanyId, id, body.Status), ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/payroll/runs/{id}?clientCompanyId=1</summary>
    [HttpDelete("runs/{id:int}")]
    public async Task<IActionResult> DeleteRun(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeletePayrollRunCommand(clientCompanyId, id), ct);
        return NoContent();
    }

    /// <summary>GET /api/v1/payroll/runs/{id}/posting?clientCompanyId=1 — ใบสำคัญลงบัญชี + กระทบยอด GL</summary>
    [HttpGet("runs/{id:int}/posting")]
    public async Task<IActionResult> GetPosting(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetPayrollPostingQuery(clientCompanyId, id), ct));

    /// <summary>GET /api/v1/payroll/runs/{id}/sso-filing?clientCompanyId=1 — ข้อมูล สปส.1-10 ต่องวด</summary>
    [HttpGet("runs/{id:int}/sso-filing")]
    public async Task<IActionResult> GetSsoFiling(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetSsoFilingQuery(clientCompanyId, id), ct));

    /// <summary>GET /api/v1/payroll/runs/{id}/sso-filing/excel?clientCompanyId=1 — ไฟล์อัปโหลด e-Service</summary>
    [HttpGet("runs/{id:int}/sso-filing/excel")]
    public async Task<IActionResult> GetSsoFilingExcel(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetSsoFilingExcelQuery(clientCompanyId, id), ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"sso1-10-{id}.xlsx");
    }

    /// <summary>GET /api/v1/payroll/runs/{id}/sso-filing/pdf?clientCompanyId=1 — ฟอร์ม สปส.1-10 (PDF)</summary>
    [HttpGet("runs/{id:int}/sso-filing/pdf")]
    public async Task<IActionResult> GetSsoFilingPdf(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetSsoFilingPdfQuery(clientCompanyId, id), ct);
        return File(bytes, "application/pdf", $"sso1-10-{id}.pdf");
    }

    /// <summary>GET /api/v1/payroll/runs/{id}/template?clientCompanyId=1 — ดาวน์โหลด Excel template ไปกรอก</summary>
    [HttpGet("runs/{id:int}/template")]
    public async Task<IActionResult> GetRunTemplate(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetPayrollRunTemplateQuery(clientCompanyId, id), ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"payroll-run-{id}.xlsx");
    }

    /// <summary>POST /api/v1/payroll/runs/{id}/import?clientCompanyId=1 (multipart: file) — อัปโหลดทับค่ารายการ</summary>
    [HttpPost("runs/{id:int}/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportRun(int id, [FromQuery] int clientCompanyId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { detail = "กรุณาเลือกไฟล์ Excel" });
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var updated = await mediator.Send(new ImportPayrollRunCommand(clientCompanyId, id, ms.ToArray()), ct);
        return Ok(new { updated });
    }

    public record CreateRunBody(int Year, int Month);
    public record SetStatusBody(int Status);

    // ── แมพบัญชีเงินเดือน (Express GL → ฝ่าย) ──────────────────────────────────
    /// <summary>GET /api/v1/payroll/account-mappings?clientCompanyId=1</summary>
    [HttpGet("account-mappings")]
    public async Task<IActionResult> GetAccountMappings([FromQuery] int clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetPayrollAccountMappingsQuery(clientCompanyId), ct));

    /// <summary>POST /api/v1/payroll/account-mappings?clientCompanyId=1 (body: {accountCode,department,note})</summary>
    [HttpPost("account-mappings")]
    public async Task<IActionResult> CreateAccountMapping([FromQuery] int clientCompanyId, [FromBody] PayrollAccountMappingInput data, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new UpsertPayrollAccountMappingCommand(clientCompanyId, null, data), ct) });

    /// <summary>PUT /api/v1/payroll/account-mappings/{id}?clientCompanyId=1</summary>
    [HttpPut("account-mappings/{id:int}")]
    public async Task<IActionResult> UpdateAccountMapping(int id, [FromQuery] int clientCompanyId, [FromBody] PayrollAccountMappingInput data, CancellationToken ct)
    {
        await mediator.Send(new UpsertPayrollAccountMappingCommand(clientCompanyId, id, data), ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/payroll/account-mappings/{id}?clientCompanyId=1</summary>
    [HttpDelete("account-mappings/{id:int}")]
    public async Task<IActionResult> DeleteAccountMapping(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeletePayrollAccountMappingCommand(clientCompanyId, id), ct);
        return NoContent();
    }
}
