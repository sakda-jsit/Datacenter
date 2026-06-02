import apiClient from '../../../shared/services/apiClient'
import type { AuditLogDto, AuditLogParams, PaginatedResult } from '../types/auditLog.types'

export const auditLogApi = {
  getLogs: (params: AuditLogParams) =>
    apiClient
      .get<PaginatedResult<AuditLogDto>>('/audit-log', { params })
      .then((r) => r.data),
}
