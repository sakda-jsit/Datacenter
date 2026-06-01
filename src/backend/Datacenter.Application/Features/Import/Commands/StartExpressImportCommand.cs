using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.Import.Commands;

/// <summary>
/// Starts an Express DBF import for one client and fiscal year.
/// Creates ImportBatch, reads DBF files, writes to staging tables, validates.
/// Returns the new ImportBatch Id.
/// </summary>
public record StartExpressImportCommand(
    int ClientCompanyId,
    int FiscalYear)
    : IRequest<int>, IRequireCompanyAccess;
