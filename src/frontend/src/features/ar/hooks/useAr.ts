import { useQuery } from '@tanstack/react-query'
import { arApi } from '../services/arApi'

const keys = {
  customers: (c: number, inc: boolean) => ['ar-customers', c, inc] as const,
  invoices: (c: number, y: number, o: boolean, cus?: string) => ['ar-invoices', c, y, o, cus ?? ''] as const,
  aging: (c: number, asOf?: string) => ['ar-aging', c, asOf ?? ''] as const,
  years: (c: number) => ['ar-years', c] as const,
}

export function useCustomers(companyId: number, includeInactive = false, enabled = true) {
  return useQuery({
    queryKey: keys.customers(companyId, includeInactive),
    queryFn: () => arApi.customers(companyId, includeInactive),
    enabled: enabled && companyId > 0,
  })
}

export function useArInvoices(companyId: number, year: number, outstandingOnly: boolean, customerCode?: string, enabled = true) {
  return useQuery({
    queryKey: keys.invoices(companyId, year, outstandingOnly, customerCode),
    queryFn: () => arApi.invoices(companyId, year, outstandingOnly, customerCode),
    enabled: enabled && companyId > 0,
  })
}

export function useArAging(companyId: number, asOf?: string, enabled = true) {
  return useQuery({
    queryKey: keys.aging(companyId, asOf),
    queryFn: () => arApi.aging(companyId, asOf),
    enabled: enabled && companyId > 0,
  })
}

export function useArYears(companyId: number, enabled = true) {
  return useQuery({
    queryKey: keys.years(companyId),
    queryFn: () => arApi.years(companyId),
    enabled: enabled && companyId > 0,
  })
}
