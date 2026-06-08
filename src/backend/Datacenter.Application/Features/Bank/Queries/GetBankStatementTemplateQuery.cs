using Datacenter.Application.Common.Interfaces;
using MediatR;

namespace Datacenter.Application.Features.Bank.Queries;

/// <summary>เทมเพลต Excel สำหรับกรอก/วาง statement (คอลัมน์คงที่)</summary>
public record GetBankStatementTemplateQuery : IRequest<byte[]>;

public class GetBankStatementTemplateQueryHandler(IBankStatementParser parser)
    : IRequestHandler<GetBankStatementTemplateQuery, byte[]>
{
    public Task<byte[]> Handle(GetBankStatementTemplateQuery request, CancellationToken ct)
        => Task.FromResult(parser.BuildTemplate());
}
