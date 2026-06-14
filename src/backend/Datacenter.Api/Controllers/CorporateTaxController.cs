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

    /// <summary>GET /corporate-tax/signers?clientCompanyId=1&amp;fiscalYear=2025 — ผู้ลงนาม (ค่าเริ่มต้น+override+resolved)</summary>
    [HttpGet("signers")]
    public async Task<IActionResult> GetSigners([FromQuery] GetCompanySignersQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>PUT /corporate-tax/signers/default?clientCompanyId=1 — ตั้งผู้ลงนามประจำบริษัท (ค่าเริ่มต้นทุกปี)</summary>
    [HttpPut("signers/default")]
    public async Task<IActionResult> SetDefaultSigners(
        [FromQuery] int clientCompanyId, [FromBody] CompanyDefaultSignersInput data, CancellationToken ct)
        => Ok(await mediator.Send(new SetCompanyDefaultSignersCommand(clientCompanyId, data), ct));

    /// <summary>PUT /corporate-tax/signers/year?clientCompanyId=1&amp;fiscalYear=2025 — override + วันที่ในรายงาน เฉพาะปี</summary>
    [HttpPut("signers/year")]
    public async Task<IActionResult> SaveYearSigners(
        [FromQuery] int clientCompanyId, [FromQuery] int fiscalYear,
        [FromBody] CompanyYearSignersInput data, CancellationToken ct)
        => Ok(await mediator.Send(new SaveCompanyYearSignersCommand(clientCompanyId, fiscalYear, data), ct));

    /// <summary>GET /corporate-tax/signer-assignments — ภาพรวมผู้ลงนามประจำของทุกบริษัท (จัดการรวมศูนย์)</summary>
    [HttpGet("signer-assignments")]
    public async Task<IActionResult> GetSignerAssignments(
        [FromQuery] string? search, [FromQuery] int? auditorId, [FromQuery] int? bookkeeperId, CancellationToken ct)
        => Ok(await mediator.Send(new GetSignerAssignmentsQuery(search, auditorId, bookkeeperId), ct));
}
