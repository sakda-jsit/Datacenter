using Datacenter.Application.Features.Import.DTOs;
using MediatR;

namespace Datacenter.Application.Features.Import.Queries;

public record GetImportValidationResultQuery(int ImportBatchId) : IRequest<ImportValidationSummaryDto>;
