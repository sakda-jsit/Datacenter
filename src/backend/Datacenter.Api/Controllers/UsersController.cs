using Datacenter.Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

/// <summary>
/// จัดการผู้ใช้ระบบ — <b>เฉพาะ Admin</b>. สร้างผู้ใช้ให้พนักงานแต่ละคน (ห้ามใช้บัญชีร่วมกัน
/// เพราะ audit log/field-audit ต้องระบุตัวผู้ทำได้) + ผูกสิทธิ์เข้าถึงรายบริษัทลูกค้า.
/// </summary>
[Authorize(Roles = AuthRoles.CentralSettings)]
[ApiController]
[Route("api/v1/users")]
public class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/users — รายชื่อผู้ใช้ + สิทธิ์บริษัท + สถานะล็อก</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await mediator.Send(new GetUsersQuery(), ct));

    /// <summary>POST /api/v1/users (body: UserCreateInput) — สร้างผู้ใช้ (ต้องเปลี่ยนรหัสตอน login ครั้งแรก)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateInput body, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new CreateUserCommand(body), ct) });

    /// <summary>PUT /api/v1/users/{id} (body: UserUpdateInput) — แก้ชื่อ/อีเมล/บทบาท/สถานะ + สิทธิ์บริษัท</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateInput body, CancellationToken ct)
    {
        await mediator.Send(new UpdateUserCommand(id, body), ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/users/{id}/reset-password (body: { newPassword }) — ผู้ดูแลตั้งรหัสชั่วคราวให้</summary>
    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest body, CancellationToken ct)
    {
        await mediator.Send(new ResetUserPasswordCommand(id, body.NewPassword), ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/users/{id}/unlock — ปลดล็อกบัญชีที่ถูกล็อกจากการใส่รหัสผิด</summary>
    [HttpPost("{id:int}/unlock")]
    public async Task<IActionResult> Unlock(int id, CancellationToken ct)
    {
        await mediator.Send(new UnlockUserCommand(id), ct);
        return NoContent();
    }

    public record ResetPasswordRequest(string NewPassword);
}
