import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { whtApi } from '../services/whtApi'

const keys = {
  report: (companyId: number, year: number) => ['wht-report', companyId, year] as const,
  years: (companyId: number) => ['wht-years', companyId] as const,
  entries: (companyId: number, year: number, month: number, formType?: number) =>
    ['wht-entries', companyId, year, month, formType ?? 0] as const,
}

export function useWhtReport(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.report(companyId, year),
    queryFn: () => whtApi.report(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

export function useWhtYears(companyId: number, enabled = true) {
  return useQuery({
    queryKey: keys.years(companyId),
    queryFn: () => whtApi.years(companyId),
    enabled: enabled && companyId > 0,
  })
}

export function useWhtEntries(
  companyId: number,
  year: number,
  month: number,
  formType: number | undefined,
  enabled = true,
) {
  return useQuery({
    queryKey: keys.entries(companyId, year, month, formType),
    queryFn: () => whtApi.entries(companyId, year, month, formType),
    enabled: enabled && companyId > 0,
  })
}

export function useSetPayeeEmail(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ taxId, email }: { taxId: string; email: string | null }) =>
      whtApi.setPayeeEmail(companyId, taxId, email),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['wht-entries', companyId] }),
  })
}

export function useSendWht(companyId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { entryIds: number[]; grouping?: number; recipientEmail?: string }) =>
      whtApi.send(companyId, vars.entryIds, vars.grouping ?? 0, vars.recipientEmail),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['wht-entries', companyId] }),
  })
}
