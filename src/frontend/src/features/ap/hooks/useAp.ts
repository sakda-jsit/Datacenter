import { useQuery } from '@tanstack/react-query'
import { apApi } from '../services/apApi'

const keys = {
  suppliers: (c: number, inc: boolean) => ['ap-suppliers', c, inc] as const,
  invoices: (c: number, y: number, o: boolean, s?: string) => ['ap-invoices', c, y, o, s ?? ''] as const,
  aging: (c: number, asOf?: string) => ['ap-aging', c, asOf ?? ''] as const,
  years: (c: number) => ['ap-years', c] as const,
}

export function useSuppliers(companyId: number, includeInactive = false, enabled = true) {
  return useQuery({
    queryKey: keys.suppliers(companyId, includeInactive),
    queryFn: () => apApi.suppliers(companyId, includeInactive),
    enabled: enabled && companyId > 0,
  })
}

export function useApInvoices(companyId: number, year: number, outstandingOnly: boolean, supplierCode?: string, enabled = true) {
  return useQuery({
    queryKey: keys.invoices(companyId, year, outstandingOnly, supplierCode),
    queryFn: () => apApi.invoices(companyId, year, outstandingOnly, supplierCode),
    enabled: enabled && companyId > 0,
  })
}

export function useApAging(companyId: number, asOf?: string, enabled = true) {
  return useQuery({
    queryKey: keys.aging(companyId, asOf),
    queryFn: () => apApi.aging(companyId, asOf),
    enabled: enabled && companyId > 0,
  })
}

export function useApYears(companyId: number, enabled = true) {
  return useQuery({
    queryKey: keys.years(companyId),
    queryFn: () => apApi.years(companyId),
    enabled: enabled && companyId > 0,
  })
}
