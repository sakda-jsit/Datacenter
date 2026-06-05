import { useQuery } from '@tanstack/react-query'
import { vatApi } from '../services/vatApi'

const keys = {
  report: (companyId: number, year: number) => ['vat-report', companyId, year] as const,
  years: (companyId: number) => ['vat-years', companyId] as const,
  entries: (companyId: number, year: number, month: number, vatType?: number) =>
    ['vat-entries', companyId, year, month, vatType ?? 0] as const,
}

export function useVatReport(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.report(companyId, year),
    queryFn: () => vatApi.report(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

export function useVatYears(companyId: number, enabled = true) {
  return useQuery({
    queryKey: keys.years(companyId),
    queryFn: () => vatApi.years(companyId),
    enabled: enabled && companyId > 0,
  })
}

export function useVatEntries(
  companyId: number,
  year: number,
  month: number,
  vatType: number | undefined,
  enabled = true,
) {
  return useQuery({
    queryKey: keys.entries(companyId, year, month, vatType),
    queryFn: () => vatApi.entries(companyId, year, month, vatType),
    enabled: enabled && companyId > 0,
  })
}
