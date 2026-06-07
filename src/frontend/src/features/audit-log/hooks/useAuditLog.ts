import { useQuery } from '@tanstack/react-query'
import { auditLogApi } from '../services/auditLogApi'
import type { AuditLogParams } from '../types/auditLog.types'

export function useAuditLogs(params: AuditLogParams) {
  return useQuery({
    queryKey: ['audit-log', params],
    queryFn: () => auditLogApi.getLogs(params),
  })
}

export function useAuditLogFilterOptions(clientCompanyId?: number) {
  return useQuery({
    queryKey: ['audit-log-filter-options', clientCompanyId ?? null],
    queryFn: () => auditLogApi.filterOptions(clientCompanyId),
  })
}
