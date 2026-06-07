import apiClient from '../../../shared/services/apiClient'
import type {
  AuditLogDto,
  AuditLogExportParams,
  AuditLogExportResult,
  AuditLogFilterOptions,
  AuditLogParams,
  PaginatedResult,
} from '../types/auditLog.types'

export const auditLogApi = {
  getLogs: (params: AuditLogParams) =>
    apiClient
      .get<PaginatedResult<AuditLogDto>>('/audit-log', { params })
      .then((r) => r.data),

  export: (params: AuditLogExportParams) =>
    apiClient
      .get<AuditLogExportResult>('/audit-log/export', { params })
      .then((r) => r.data),

  filterOptions: (clientCompanyId?: number) =>
    apiClient
      .get<AuditLogFilterOptions>('/audit-log/filter-options', { params: { clientCompanyId } })
      .then((r) => r.data),
}
