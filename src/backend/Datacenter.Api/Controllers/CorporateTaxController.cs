using Datacenter.Application.Features.CorporateTax.Commands;
using Datacenter.Application.Features.CorporateTax.DTOs;
using Datacenter.Application.Features.CorporateTax.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

/// <summary>
/// คำนวณภาษีเงินได้นิติบุคคล (ภ.ง.ด.50) จากกำไรสุทธิทางบัญชี + รายการปรับปรุงทางภาษี.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/corporate-tax")]
public class CorporateTaxController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/corporate-tax/computation?clientCompanyId=1&amp;fiscalYear=2025 — กระดาษทำการ + ผลคำนวณ</summary>
    [HttpGet("computation")]
    public async Task<IActionResult> GetComputation([FromQuery] GetTaxComputationQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>PUT /api/v1/corporate-tax/computation?clientCompanyId=1&amp;fiscalYear=2025 — บันทึกกระดาษทำการ + lง X4 ในงบ</summary>
    [HttpPut("computation")]
    public async Task<IActionResult> SaveComputation(
        [FromQuery] int clientCompanyId, [FromQuery] int fiscalYear,
        [FromBody] TaxComputationInput data, CancellationToken ct)
        => Ok(await mediator.Send(new SaveTaxComputationCommand(clientCompanyId, fiscalYear, data), ct));

    /// <summary>GET /api/v1/corporate-tax/pnd50-pdf?clientCompanyId=1&amp;fiscalYear=2025 — แบบ ภ.ง.ด.50 (PDF)</summary>
    [HttpGet("pnd50-pdf")]
    public async Task<IActionResult> GetPnd50Pdf([FromQuery] int clientCompanyId, [FromQuery] int fiscalYear, CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetPnd50PdfQuery(clientCompanyId, fiscalYear), ct);
        return File(bytes, "application/pdf", $"pnd50-{clientCompanyId}-{fiscalYear + 543}.pdf");
    }

    /// <summary>GET /api/v1/corporate-tax/auditor?clientCompanyId=1&amp;fiscalYear=2025 — ผู้สอบบัญชีของรอบปีนี้</summary>
    [HttpGet("auditor")]
    public async Task<IActionResult> GetAuditor([FromQuery] GetCompanyAuditorQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>PUT /api/v1/corporate-tax/auditor?clientCompanyId=1&amp;fiscalYear=2025 — บันทึกผู้สอบบัญชีของรอบปีนี้</summary>
    [HttpPut("auditor")]
    public async Task<IActionResult> SaveAuditor(
        [FromQuery] int clientCompanyId, [FromQuery] int fiscalYear,
        [FromBody] CompanyAuditorInput data, CancellationToken ct)
        => Ok(await mediator.Send(new SaveCompanyAuditorCommand(clientCompanyId, fiscalYear, data), ct));
}
