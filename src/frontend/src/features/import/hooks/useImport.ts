import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { importApi } from '../services/importApi'
import type { StartExpressImportRequest } from '../types/import.types'

const IMPORT_KEY = 'import'

export function useImportHistory(params: {
  clientCompanyId?: number
  fiscalYear?: number
  pageNumber?: number
  pageSize?: number
}) {
  return useQuery({
    queryKey: [IMPORT_KEY, 'history', params],
    queryFn: () => importApi.getHistory(params),
  })
}

export function useImportValidation(id: number) {
  return useQuery({
    queryKey: [IMPORT_KEY, 'validation', id],
    queryFn: () => importApi.getValidation(id),
    enabled: id > 0,
  })
}

export function useStartExpressImport() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: StartExpressImportRequest) => importApi.startExpressImport(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: [IMPORT_KEY] }),
  })
}
