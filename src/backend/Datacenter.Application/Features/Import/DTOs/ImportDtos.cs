using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Import.DTOs;

public record ImportBatchListDto(
    int Id,
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    ImportSourceType SourceType,
    string ImportType,
    int FiscalYear,
    ImportStatus Status,
    int TotalRows,
    int SuccessRows,
    int ErrorRows,
    string? Message,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? FinishedAt,
    bool IsPosted,
    DateTime? PostedAt);

public record PostImportResultDto(
    int ImportBatchId,
    int FiscalYear,
    int AccountsUpserted,
    int OpeningLines,
    int MovementLines,
    string Message);

public record ImportBatchDetailDto(
    long Id,
    int RowNumber,
    string? AccountCode,
    bool IsValid,
    string? ErrorMessage,
    string RawData);

public record ImportValidationSummaryDto(
    int ImportBatchId,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<ImportBatchDetailDto> Errors);
