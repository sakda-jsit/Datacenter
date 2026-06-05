using Datacenter.Application.Features.FinancialStatement.Commands;
using Datacenter.Application.Features.FinancialStatement.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/financial-statement")]
public class FinancialStatementController(IMediator mediator) : ControllerBase
{
    // ── Reports ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/financial-statement/balance-sheet?clientCompanyId=1&fiscalYear=2024</summary>
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] GetBalanceSheetQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/financial-statement/profit-loss?clientCompanyId=1&fiscalYear=2024</summary>
    [HttpGet("profit-loss")]
    public async Task<IActionResult> GetProfitLoss([FromQuery] GetProfitLossQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/financial-statement/equity-changes?clientCompanyId=1&amp;fiscalYear=2024 — งบแสดงการเปลี่ยนแปลงส่วนผู้ถือหุ้น (CAP)</summary>
    [HttpGet("equity-changes")]
    public async Task<IActionResult> GetEquityChanges([FromQuery] GetEquityChangesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    // ── NOTE2 (หมายเหตุประกอบงบการเงิน) ────────────────────────────────────────

    /// <summary>GET /api/v1/financial-statement/notes?clientCompanyId=1&amp;fiscalYear=2024 — NOTE2 ฉบับเต็ม</summary>
    [HttpGet("notes")]
    public async Task<IActionResult> GetNotes([FromQuery] GetNotesToFsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/financial-statement/notes/excel?clientCompanyId=1&amp;fiscalYear=2024 — NOTE2 รูปแบบงบ (.xlsx)</summary>
    [HttpGet("notes/excel")]
    public async Task<IActionResult> GetNotesExcel([FromQuery] GetNotesExcelQuery query, CancellationToken ct)
    {
        var bytes = await mediator.Send(query, ct);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"NOTE2-{query.ClientCompanyId}-{query.FiscalYear}.xlsx");
    }

    /// <summary>GET /api/v1/financial-statement/note-templates?clientCompanyId=1&amp;fiscalYear=2024 — ข้อความ template ที่มีผล (สำหรับแก้ไข)</summary>
    [HttpGet("note-templates")]
    public async Task<IActionResult> GetNoteTemplates([FromQuery] GetNoteTemplateSectionsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>PUT /api/v1/financial-statement/note-templates — บันทึกข้อความ NOTE2 เฉพาะบริษัท (override)</summary>
    [HttpPut("note-templates")]
    public async Task<IActionResult> UpsertNoteTemplate([FromBody] UpsertNoteTemplateSectionCommand command, CancellationToken ct)
        => Ok(new { id = await mediator.Send(command, ct) });

    /// <summary>POST /api/v1/financial-statement/note-templates/reset — ลบ override กลับไปใช้ template กลาง</summary>
    [HttpPost("note-templates/reset")]
    public async Task<IActionResult> ResetNoteTemplate([FromBody] ResetNoteTemplateSectionCommand command, CancellationToken ct)
        => Ok(new { reset = await mediator.Send(command, ct) });

    // ── Account Mappings ──────────────────────────────────────────────────────

    /// <summary>GET /api/v1/financial-statement/mappings?clientCompanyId=1</summary>
    [HttpGet("mappings")]
    public async Task<IActionResult> GetMappings([FromQuery] GetAccountMappingsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>PUT /api/v1/financial-statement/mappings</summary>
    [HttpPut("mappings")]
    public async Task<IActionResult> UpsertMapping([FromBody] UpsertAccountMappingCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/financial-statement/mappings/{clientCompanyId}/{accountCode}</summary>
    [HttpDelete("mappings/{clientCompanyId:int}/{accountCode}")]
    public async Task<IActionResult> DeleteMapping(int clientCompanyId, string accountCode, CancellationToken ct)
    {
        await mediator.Send(new DeleteAccountMappingCommand(clientCompanyId, accountCode), ct);
        return NoContent();
    }

    // ── External Inputs (X4 income tax, WHT prepaid tax applied) ───────────────

    /// <summary>GET /api/v1/financial-statement/external-inputs?clientCompanyId=1&amp;fiscalYear=2024</summary>
    [HttpGet("external-inputs")]
    public async Task<IActionResult> GetExternalInputs([FromQuery] GetExternalInputsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>PUT /api/v1/financial-statement/external-inputs</summary>
    [HttpPut("external-inputs")]
    public async Task<IActionResult> UpsertExternalInput([FromBody] UpsertExternalInputCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }
}
