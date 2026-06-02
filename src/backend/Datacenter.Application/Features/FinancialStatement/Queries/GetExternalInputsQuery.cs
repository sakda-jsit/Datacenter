using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

/// <summary>
/// Reads back the manually-entered external inputs (e.g. X4 income tax, WHT prepaid tax applied)
/// for a company + fiscal year, so the ภงด.50 form can pre-fill saved values.
/// </summary>
public record GetExternalInputsQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<IReadOnlyList<FsExternalInputDto>>, IRequireCompanyAccess;
