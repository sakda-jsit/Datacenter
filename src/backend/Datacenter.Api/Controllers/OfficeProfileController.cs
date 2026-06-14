using Datacenter.Application.Features.OfficeProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

/// <summary>
/// โปรไฟล์สำนักงานบัญชีของผู้ใช้ระบบ — <b>ค่ากลาง</b> (ไม่แยกบริษัทลูกค้า).
/// ใช้เติม "สำนักงานทำบัญชี" ในแบบ ภ.ง.ด.50 + ข้อมูลสำนักงานสำหรับเอกสารอื่น. อยู่ในเมนูตั้งค่ากลาง.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/office-profile")]
public class OfficeProfileController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/office-profile — โปรไฟล์สำนักงาน (ค่าว่างถ้ายังไม่ตั้ง)</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await mediator.Send(new GetOfficeProfileQuery(), ct));

    /// <summary>PUT /api/v1/office-profile (body: OfficeProfileInput) — บันทึกโปรไฟล์สำนักงาน</summary>
    [HttpPut]
    public async Task<IActionResult> Save([FromBody] OfficeProfileInput data, CancellationToken ct)
        => Ok(await mediator.Send(new SaveOfficeProfileCommand(data), ct));
}
