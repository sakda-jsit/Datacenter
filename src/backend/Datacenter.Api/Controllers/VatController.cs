using Datacenter.Application.Features.Vat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/vat")]
public class VatController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/vat/report?clientCompanyId=1&amp;year=2025 — รายงาน ภ.พ.30 รายเดือน</summary>
    [HttpGet("report")]
    public async Task<IActionResult> GetReport([FromQuery] GetVatReportQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/vat/years?clientCompanyId=1 — ปีภาษีที่มีข้อมูล</summary>
    [HttpGet("years")]
    public async Task<IActionResult> GetYears([FromQuery] GetVatYearsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/vat?clientCompanyId=1&amp;year=2025&amp;month=0&amp;vatType= — รายละเอียดภาษีซื้อ/ขาย</summary>
    [HttpGet]
    public async Task<IActionResult> GetEntries([FromQuery] GetVatEntriesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/vat/tax-report/excel?clientCompanyId=1&amp;year=2025&amp;vatType=1&amp;month=0 — รายงานภาษีขาย/ซื้อ (Excel)</summary>
    [HttpGet("tax-report/excel")]
    public async Task<IActionResult> GetTaxReportExcel([FromQuery] int clientCompanyId, [FromQuery] int year, [FromQuery] int vatType, [FromQuery] int month, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetVatTaxReportExcelQuery(clientCompanyId, year, vatType, month), ct);
        var kind = vatType == 1 ? "sales" : "purchase";
        var name = month > 0 ? $"vat-{kind}-{year + 543}-{month:D2}.xlsx" : $"vat-{kind}-{year + 543}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    /// <summary>GET /api/v1/vat/pp30-branches?clientCompanyId=1&amp;year=2025&amp;month=1 — ยอด ภ.พ.30 แยกตามสาขา (DEPCOD)</summary>
    [HttpGet("pp30-branches")]
    public async Task<IActionResult> GetPp30Branches([FromQuery] GetVatPp30BranchesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/vat/pp30-transfer?clientCompanyId=1&amp;year=2025&amp;month=1 — ไฟล์โอนย้าย ภ.พ.30 (.txt) สำหรับอัปโหลด e-Filing</summary>
    [HttpGet("pp30-transfer")]
    public async Task<IActionResult> GetPp30Transfer(
        [FromQuery] int clientCompanyId, [FromQuery] int year, [FromQuery] int month,
        [FromQuery] string delimiter = "|", [FromQuery] bool includeHeader = true, CancellationToken ct = default)
    {
        var bytes = await mediator.Send(
            new GetVatPp30TransferQuery(clientCompanyId, year, month, delimiter, includeHeader), ct);
        return File(bytes, "text/plain", $"pp30-transfer-{year + 543}-{month:D2}.txt");
    }
}
