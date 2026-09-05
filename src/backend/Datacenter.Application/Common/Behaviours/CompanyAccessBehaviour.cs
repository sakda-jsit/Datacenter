using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Common.Behaviours;

/// <summary>
/// Pipeline behaviour ที่บังคับใช้ multi-company isolation แบบรวมศูนย์
/// ทุก request ที่ implement <see cref="IRequireCompanyAccess"/> จะถูกตรวจสิทธิ์
/// ก่อนเข้า handler ทำให้ไม่ต้องเขียนเช็กสิทธิ์ซ้ำในแต่ละ handler
/// </summary>
public class CompanyAccessBehaviour<TRequest, TResponse>(ICompanyAccessGuard guard)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ผู้ดูแลบริษัท (ทำรายการ / ดูเงินเดือน) ตรวจเข้มกว่า — เช็คก่อนเพราะ interface สืบทอดกัน
        if (request is IRequireCompanyOwnerAccess owned)
            await guard.EnsureOwnerAccessAsync(owned.ClientCompanyId, cancellationToken);
        else if (request is IRequireCompanyAccess scoped)
            await guard.EnsureAccessAsync(scoped.ClientCompanyId, cancellationToken);

        return await next();
    }
}
