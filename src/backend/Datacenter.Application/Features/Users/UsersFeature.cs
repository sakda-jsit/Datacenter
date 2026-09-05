using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Users;

// ── จัดการผู้ใช้ระบบ (เฉพาะ Admin) ─────────────────────────────────────────────
// ผู้ใช้ = พนักงานสำนักงานบัญชี. สิทธิ์เข้าถึงข้อมูลรายบริษัทลูกค้าเก็บที่ CompanyUserAccess
// (Admin เห็นทุกบริษัทโดยไม่ต้องผูก — ดู CompanyAccessGuard). role serialize เป็นตัวเลขตามแบบของ API นี้.

public record UserDto(
    int Id,
    string Username,
    string DisplayName,
    string? Email,
    int Role,
    bool IsActive,
    bool MustChangePassword,
    DateTime? LastLoginAt,
    DateTime? LockedUntil,
    bool IsLocked,
    IReadOnlyList<int> CompanyIds);

public record UserCreateInput(
    string Username,
    string DisplayName,
    string? Email,
    int Role,
    string Password,
    IReadOnlyList<int>? CompanyIds);

public record UserUpdateInput(
    string DisplayName,
    string? Email,
    int Role,
    bool IsActive,
    IReadOnlyList<int>? CompanyIds);

// ── รายชื่อผู้ใช้ ──
public record GetUsersQuery : IRequest<IReadOnlyList<UserDto>>;

public class GetUsersQueryHandler(IApplicationDbContext db) : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var users = await db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync(ct);
        var access = await db.CompanyUserAccesses.AsNoTracking().ToListAsync(ct);

        return users.Select(u => new UserDto(
            u.Id, u.Username, u.DisplayName, u.Email, (int)u.Role, u.IsActive, u.MustChangePassword,
            u.LastLoginAt, u.LockedUntil, u.LockedUntil is not null && u.LockedUntil > now,
            access.Where(a => a.UserId == u.Id).Select(a => a.ClientCompanyId).OrderBy(x => x).ToList()))
            .ToList();
    }
}

// ── สร้างผู้ใช้ (บังคับเปลี่ยนรหัสตอน login ครั้งแรก) ──
public record CreateUserCommand(UserCreateInput Data) : IRequest<int>;

public class CreateUserCommandHandler(
    IApplicationDbContext db, IPasswordHasher hasher, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<CreateUserCommand, int>
{
    public async Task<int> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var d = req.Data;
        var username = (d.Username ?? "").Trim();

        if (await db.Users.AnyAsync(u => u.Username == username, ct))
            throw new DomainException($"ชื่อผู้ใช้ {username} มีอยู่แล้ว");

        PasswordPolicy.EnsureValid(d.Password, username, "password");
        var role = UsersFeatureHelpers.ParseRole(d.Role);
        UsersFeatureHelpers.EnsureMayAssignRole(currentUser, role);

        var user = new User
        {
            Username = username,
            PasswordHash = hasher.Hash(d.Password),
            DisplayName = string.IsNullOrWhiteSpace(d.DisplayName) ? username : d.DisplayName.Trim(),
            Email = string.IsNullOrWhiteSpace(d.Email) ? null : d.Email.Trim(),
            Role = role,
            IsActive = true,
            MustChangePassword = true,   // ผู้ดูแลตั้งรหัสให้ → เจ้าตัวต้องเปลี่ยนก่อนใช้งาน
            CreatedBy = currentUser.Username,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        await UsersFeatureHelpers.ReplaceCompanyAccessAsync(db, user, d.CompanyIds, ct);
        await audit.LogAsync("Create", "User", user.Id.ToString(),
            afterValue: $"{user.Username} / {role} / บริษัท {d.CompanyIds?.Count ?? 0} แห่ง", cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return user.Id;
    }
}

// ── แก้ไขผู้ใช้ (ชื่อผู้ใช้แก้ไม่ได้ — คงไว้เพื่อให้ audit trail ตามรอยได้) ──
public record UpdateUserCommand(int Id, UserUpdateInput Data) : IRequest<Unit>;

public class UpdateUserCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UpdateUserCommand, Unit>
{
    public async Task<Unit> Handle(UpdateUserCommand req, CancellationToken ct)
    {
        var d = req.Data;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.Id, ct)
            ?? throw new NotFoundException("ผู้ใช้", req.Id);

        var role = UsersFeatureHelpers.ParseRole(d.Role);
        UsersFeatureHelpers.EnsureMayManage(currentUser, user);
        if (role != user.Role)   // ตรวจเฉพาะตอนเปลี่ยนบทบาท — คงบทบาทเดิมไว้ได้เสมอ (รวมถึงหัวหน้างานแก้บัญชีตัวเอง)
            UsersFeatureHelpers.EnsureMayAssignRole(currentUser, role);
        var before = $"{user.DisplayName} / {user.Role} / {(user.IsActive ? "ใช้งาน" : "ปิดใช้งาน")}";

        // กันล็อกตัวเองออกจากระบบ และกันไม่ให้เหลือ Admin ที่ใช้งานได้ 0 คน
        if (req.Id == currentUser.UserId && (!d.IsActive || role != user.Role))
            throw new DomainException("ไม่สามารถปิดใช้งานหรือเปลี่ยนบทบาทของบัญชีตัวเองได้ — ให้ผู้ดูแลอีกคนดำเนินการ");

        if (user.Role == UserRole.Admin && (role != UserRole.Admin || !d.IsActive))
        {
            var otherAdmins = await db.Users
                .CountAsync(u => u.Id != user.Id && u.Role == UserRole.Admin && u.IsActive, ct);
            if (otherAdmins == 0)
                throw new DomainException("ต้องมีผู้ดูแลระบบ (Admin) ที่ใช้งานได้อย่างน้อย 1 บัญชี");
        }

        user.DisplayName = string.IsNullOrWhiteSpace(d.DisplayName) ? user.Username : d.DisplayName.Trim();
        user.Email = string.IsNullOrWhiteSpace(d.Email) ? null : d.Email.Trim();
        user.Role = role;
        user.IsActive = d.IsActive;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = currentUser.Username;

        // ปิดใช้งาน = ตัดการเข้าใช้งานที่ค้างอยู่ทันที (refresh token ทุกใบ)
        if (!d.IsActive)
        {
            var active = await db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(ct);
            foreach (var t in active) { t.RevokedAt = DateTime.UtcNow; t.RevokedReason = "user-deactivated"; }
        }

        await UsersFeatureHelpers.ReplaceCompanyAccessAsync(db, user, d.CompanyIds, ct);
        await audit.LogAsync("Update", "User", user.Id.ToString(),
            beforeValue: before,
            afterValue: $"{user.DisplayName} / {user.Role} / {(user.IsActive ? "ใช้งาน" : "ปิดใช้งาน")} / บริษัท {d.CompanyIds?.Count ?? 0} แห่ง",
            cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── รีเซ็ตรหัสผ่าน (ผู้ดูแลตั้งรหัสชั่วคราว → ผู้ใช้ต้องเปลี่ยนตอน login) ──
public record ResetUserPasswordCommand(int Id, string NewPassword) : IRequest<Unit>;

public class ResetUserPasswordCommandHandler(
    IApplicationDbContext db, IPasswordHasher hasher, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<ResetUserPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ResetUserPasswordCommand req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.Id, ct)
            ?? throw new NotFoundException("ผู้ใช้", req.Id);
        UsersFeatureHelpers.EnsureMayManage(currentUser, user);

        PasswordPolicy.EnsureValid(req.NewPassword, user.Username, "newPassword");

        var now = DateTime.UtcNow;
        user.PasswordHash = hasher.Hash(req.NewPassword);
        user.MustChangePassword = true;
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.ModifiedAt = now;
        user.ModifiedBy = currentUser.Username;

        var active = await db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null).ToListAsync(ct);
        foreach (var t in active) { t.RevokedAt = now; t.RevokedReason = "password-reset"; }

        await audit.LogAsync("ResetPassword", "User", user.Id.ToString(),
            afterValue: $"ผู้ดูแลรีเซ็ตรหัสผ่านของ {user.Username} (ต้องเปลี่ยนรหัสตอนเข้าใช้งาน)", cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── ปลดล็อกบัญชีที่ถูกล็อกจากการใส่รหัสผิด ──
public record UnlockUserCommand(int Id) : IRequest<Unit>;

public class UnlockUserCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UnlockUserCommand, Unit>
{
    public async Task<Unit> Handle(UnlockUserCommand req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.Id, ct)
            ?? throw new NotFoundException("ผู้ใช้", req.Id);
        UsersFeatureHelpers.EnsureMayManage(currentUser, user);

        user.LockedUntil = null;
        user.FailedLoginCount = 0;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = currentUser.Username;

        await audit.LogAsync("Unlock", "User", user.Id.ToString(),
            afterValue: $"ปลดล็อกบัญชี {user.Username}", cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

internal static class UsersFeatureHelpers
{
    /// <summary>บทบาทระดับพนักงานที่หัวหน้างานดูแลได้</summary>
    private static readonly UserRole[] SupervisorManageable = [UserRole.Maker, UserRole.Checker];

    /// <summary>
    /// หัวหน้างานแก้ไข/รีเซ็ตรหัส/ปลดล็อก ได้เฉพาะ <b>บัญชีตัวเอง</b> และบัญชี<b>ระดับพนักงาน</b>
    /// (Maker/Checker) — แตะบัญชี Admin หรือหัวหน้างานคนอื่นไม่ได้.
    /// การเปลี่ยนบทบาท/ปิดใช้งานบัญชีตัวเองยังถูกกันแยกในตัว handler (กันเลื่อนตัวเองเป็น Admin)
    /// </summary>
    public static void EnsureMayManage(ICurrentUserService currentUser, User target)
    {
        if (currentUser.Role != UserRole.Supervisor) return;   // Admin ผ่าน; บทบาทอื่นถูกกันที่ controller แล้ว

        if (target.Id == currentUser.UserId) return;           // บัญชีตัวเอง — จัดการได้

        if (!SupervisorManageable.Contains(target.Role))
            throw new ForbiddenException(
                "หัวหน้างานจัดการได้เฉพาะบัญชีของตัวเอง กับบัญชีผู้บันทึก (Maker) และผู้ตรวจ (Checker) — บัญชีอื่นให้ผู้ดูแลระบบดำเนินการ");
    }

    /// <summary>ตรวจบทบาทปลายทางที่กำลังจะตั้ง (ใช้ทั้งตอนสร้างและตอนแก้)</summary>
    public static void EnsureMayAssignRole(ICurrentUserService currentUser, UserRole role)
    {
        if (currentUser.Role != UserRole.Supervisor) return;

        if (!SupervisorManageable.Contains(role))
            throw new ForbiddenException("หัวหน้างานตั้งบทบาทได้เฉพาะผู้บันทึก (Maker) และผู้ตรวจ (Checker)");
    }

    public static UserRole ParseRole(int role) => role switch
    {
        1 => UserRole.Admin,
        2 => UserRole.Maker,
        3 => UserRole.Checker,
        4 => UserRole.Supervisor,
        _ => throw new DomainException("บทบาทผู้ใช้ไม่ถูกต้อง (1=Admin, 2=Maker, 3=Checker, 4=Supervisor)"),
    };

    /// <summary>แทนที่สิทธิ์เข้าถึงบริษัททั้งชุด (null = ไม่แก้). ยังไม่ SaveChanges ให้ผู้เรียกรวมบันทึกทีเดียว</summary>
    public static async Task ReplaceCompanyAccessAsync(
        IApplicationDbContext db, User user, IReadOnlyList<int>? companyIds, CancellationToken ct)
    {
        if (companyIds is null) return;

        var wanted = companyIds.Distinct().ToList();
        if (wanted.Count > 0)
        {
            var existing = await db.ClientCompanies.CountAsync(c => wanted.Contains(c.Id), ct);
            if (existing != wanted.Count)
                throw new DomainException("มีรหัสบริษัทที่ไม่พบในระบบ");
        }

        var current = await db.CompanyUserAccesses.Where(a => a.UserId == user.Id).ToListAsync(ct);
        db.CompanyUserAccesses.RemoveRange(current.Where(a => !wanted.Contains(a.ClientCompanyId)));

        foreach (var id in wanted.Where(id => current.All(a => a.ClientCompanyId != id)))
            db.CompanyUserAccesses.Add(new CompanyUserAccess
            {
                UserId = user.Id,
                ClientCompanyId = id,
                RoleInCompany = user.Role,
            });
    }
}

public class UserCreateInputValidator : AbstractValidator<UserCreateInput>
{
    public UserCreateInputValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("กรุณากรอกชื่อผู้ใช้")
            .MaximumLength(100)
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("ชื่อผู้ใช้ใช้ได้เฉพาะ a-z 0-9 จุด ขีดล่าง ขีดกลาง (ไม่มีเว้นวรรค)");
        RuleFor(x => x.DisplayName).MaximumLength(150);
        RuleFor(x => x.Email).MaximumLength(256)
            .Must(v => string.IsNullOrWhiteSpace(v) || v.Contains('@')).WithMessage("อีเมลไม่ถูกต้อง");
        RuleFor(x => x.Role).InclusiveBetween(1, 4).WithMessage("บทบาทผู้ใช้ไม่ถูกต้อง (1=Admin, 2=Maker, 3=Checker, 4=Supervisor)");
    }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
        => RuleFor(x => x.Data).NotNull().SetValidator(new UserCreateInputValidator());
}

public class UserUpdateInputValidator : AbstractValidator<UserUpdateInput>
{
    public UserUpdateInputValidator()
    {
        RuleFor(x => x.DisplayName).MaximumLength(150);
        RuleFor(x => x.Email).MaximumLength(256)
            .Must(v => string.IsNullOrWhiteSpace(v) || v.Contains('@')).WithMessage("อีเมลไม่ถูกต้อง");
        RuleFor(x => x.Role).InclusiveBetween(1, 4).WithMessage("บทบาทผู้ใช้ไม่ถูกต้อง (1=Admin, 2=Maker, 3=Checker, 4=Supervisor)");
    }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new UserUpdateInputValidator());
    }
}
