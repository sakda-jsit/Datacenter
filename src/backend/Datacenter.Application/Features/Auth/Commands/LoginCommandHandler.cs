using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Auth.Commands;

public class LoginCommandHandler(
    IApplicationDbContext db,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, ct);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง");

        var token = jwtTokenService.GenerateToken(user);

        return new LoginResult(
            UserId:      user.Id,
            Username:    user.Username,
            DisplayName: user.DisplayName,
            Role:        user.Role.ToString(),
            Token:       token);
    }
}
