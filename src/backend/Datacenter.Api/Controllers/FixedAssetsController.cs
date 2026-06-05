using Datacenter.Application.Features.FixedAssets.Commands;
using Datacenter.Application.Features.FixedAssets.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/fixed-assets")]
public class FixedAssetsController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/fixed-assets/asset-types — มาสเตอร์ประเภทสินทรัพย์ + อัตราค่าเสื่อม</summary>
    [HttpGet("asset-types")]
    public async Task<IActionResult> GetAssetTypes(CancellationToken ct)
        => Ok(await mediator.Send(new GetAssetTypesQuery(), ct));

    /// <summary>GET /api/v1/fixed-assets?clientCompanyId=1 — รายการสินทรัพย์</summary>
    [HttpGet]
    public async Task<IActionResult> GetAssets([FromQuery] GetFixedAssetsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/fixed-assets/workpaper?clientCompanyId=1&amp;fiscalYear=2025 — กระดาษทำการ + เทียบ GL</summary>
    [HttpGet("workpaper")]
    public async Task<IActionResult> GetWorkpaper([FromQuery] GetFixedAssetWorkpaperQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/fixed-assets/{id}?clientCompanyId=1&amp;fiscalYear=2025 — รายละเอียด + ตารางค่าเสื่อม 2 ชุด</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAsset(int id, [FromQuery] int clientCompanyId, [FromQuery] int fiscalYear, CancellationToken ct)
        => Ok(await mediator.Send(new GetFixedAssetQuery(clientCompanyId, id, fiscalYear), ct));

    /// <summary>POST /api/v1/fixed-assets — สร้างสินทรัพย์</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFixedAssetCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>PUT /api/v1/fixed-assets/{id} — แก้ไขสินทรัพย์ (รวมจำหน่าย/ขาย)</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFixedAssetCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("id ใน URL ไม่ตรงกับ body");
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/v1/fixed-assets/{id}?clientCompanyId=1 — ลบสินทรัพย์</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteFixedAssetCommand(id, clientCompanyId), ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/fixed-assets/generate-adjustment — สร้างรายการปรับปรุงค่าเสื่อมรับรู้ในปี</summary>
    [HttpPost("generate-adjustment")]
    public async Task<IActionResult> GenerateAdjustment([FromBody] GenerateDepreciationAdjustmentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>POST /api/v1/fixed-assets/generate-disposal — สร้างรายการตัดจำหน่าย/ขายสินทรัพย์เข้า TB</summary>
    [HttpPost("generate-disposal")]
    public async Task<IActionResult> GenerateDisposal([FromBody] GenerateDisposalAdjustmentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>GET /api/v1/fixed-assets/account-mappings?clientCompanyId=1 — แมพหมวด→บัญชี GL</summary>
    [HttpGet("account-mappings")]
    public async Task<IActionResult> GetAccountMappings([FromQuery] GetAssetAccountMappingsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>PUT /api/v1/fixed-assets/account-mappings — บันทึกแมพหมวด→บัญชี GL</summary>
    [HttpPut("account-mappings")]
    public async Task<IActionResult> UpsertAccountMappings([FromBody] UpsertAssetAccountMappingsCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));
}
