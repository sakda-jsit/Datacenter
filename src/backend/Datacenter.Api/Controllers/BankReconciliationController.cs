using Datacenter.Application.Features.Bank.Commands;
using Datacenter.Application.Features.Bank.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/bank-reconciliation")]
public class BankReconciliationController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /template — เทมเพลต Excel สำหรับกรอก statement</summary>
    [HttpGet("template")]
    public async Task<IActionResult> GetTemplate(CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetBankStatementTemplateQuery(), ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bank-statement-template.xlsx");
    }

    /// <summary>POST /upload?clientCompanyId=&amp;bankAccountId=&amp;previewOnly= — อัปโหลด statement (PDF/Excel/CSV)</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromQuery] int clientCompanyId,
        [FromQuery] int bankAccountId,
        [FromQuery] bool previewOnly,
        IFormFile file,
        [FromForm] decimal? openingBalance,
        [FromForm] decimal? closingBalance,
        [FromForm] string? note,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("ไฟล์ว่าง");
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        if (previewOnly)
            return Ok(await mediator.Send(new ParseBankStatementPreviewCommand(clientCompanyId, bankAccountId, file.FileName, bytes), ct));

        var id = await mediator.Send(new UploadBankStatementCommand(
            clientCompanyId, bankAccountId, file.FileName, bytes, openingBalance, closingBalance, note), ct);
        return Ok(new { id });
    }

    /// <summary>GET /imports?clientCompanyId=&amp;bankAccountId=</summary>
    [HttpGet("imports")]
    public async Task<IActionResult> GetImports([FromQuery] int clientCompanyId, [FromQuery] int? bankAccountId, CancellationToken ct)
        => Ok(await mediator.Send(new GetBankStatementImportsQuery(clientCompanyId, bankAccountId), ct));

    /// <summary>GET /{importId}?clientCompanyId= — รายงานกระทบยอด</summary>
    [HttpGet("{importId:int}")]
    public async Task<IActionResult> GetReconciliation(int importId, [FromQuery] int clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetBankReconciliationQuery(clientCompanyId, importId), ct));

    /// <summary>POST /{importId}/match — จับคู่บรรทัดกับรายการในสมุดเอง</summary>
    [HttpPost("{importId:int}/match")]
    public async Task<IActionResult> Match(int importId, [FromBody] MatchBankLineBody body, CancellationToken ct)
    {
        await mediator.Send(new MatchBankLineCommand(body.ClientCompanyId, importId, body.StatementLineId, body.BankTransactionId), ct);
        return NoContent();
    }

    /// <summary>POST /{importId}/unmatch — ปลดคู่</summary>
    [HttpPost("{importId:int}/unmatch")]
    public async Task<IActionResult> Unmatch(int importId, [FromBody] UnmatchBankLineBody body, CancellationToken ct)
    {
        await mediator.Send(new UnmatchBankLineCommand(body.ClientCompanyId, importId, body.StatementLineId), ct);
        return NoContent();
    }

    /// <summary>DELETE /imports/{id}?clientCompanyId=</summary>
    [HttpDelete("imports/{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteBankStatementImportCommand(clientCompanyId, id), ct);
        return NoContent();
    }

    /// <summary>POST /generate-adjustment — สร้างรายการปรับปรุงจากรายการ statement ที่สมุดยังไม่ลง</summary>
    [HttpPost("generate-adjustment")]
    public async Task<IActionResult> GenerateAdjustment([FromBody] GenerateBankReconciliationAdjustmentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    public record MatchBankLineBody(int ClientCompanyId, int StatementLineId, int BankTransactionId);
    public record UnmatchBankLineBody(int ClientCompanyId, int StatementLineId);
}
