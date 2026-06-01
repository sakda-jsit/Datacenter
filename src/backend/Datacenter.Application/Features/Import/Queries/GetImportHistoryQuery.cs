using Datacenter.Application.Common.Models;
using Datacenter.Application.Features.Import.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Import.Queries;

public record GetImportHistoryQuery(
    int? ClientCompanyId = null,
    int? FiscalYear = null,
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PaginatedResult<ImportBatchListDto>>;
