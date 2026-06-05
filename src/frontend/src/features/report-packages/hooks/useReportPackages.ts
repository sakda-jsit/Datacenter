import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { reportPackageApi } from '../services/reportPackageApi'

const KEY = 'report-packages'

export function useReportPackages(companyId: number, year = 0, enabled = true) {
  return useQuery({
    queryKey: [KEY, companyId, year],
    queryFn: () => reportPackageApi.list(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

function useInvalidate(companyId: number) {
  const qc = useQueryClient()
  return () => qc.invalidateQueries({ queryKey: [KEY, companyId] })
}

export function useCreateReportPackage(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: ({ fiscalYear, title }: { fiscalYear: number; title?: string }) =>
      reportPackageApi.create(companyId, fiscalYear, title),
    onSuccess: invalidate,
  })
}

export function useSetReportPackageStatus(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: ({ id, targetStatus }: { id: number; targetStatus: number }) =>
      reportPackageApi.setStatus(companyId, id, targetStatus),
    onSuccess: invalidate,
  })
}

export function useDeleteReportPackage(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (id: number) => reportPackageApi.remove(companyId, id),
    onSuccess: invalidate,
  })
}
