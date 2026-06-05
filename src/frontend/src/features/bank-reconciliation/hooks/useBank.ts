import { useQuery } from '@tanstack/react-query'
import { bankApi } from '../services/bankApi'

export function useBankAccounts(companyId: number, enabled = true) {
  return useQuery({
    queryKey: ['bank-accounts', companyId],
    queryFn: () => bankApi.accounts(companyId),
    enabled: enabled && companyId > 0,
  })
}

export function useBankBook(companyId: number, bankAccountCode: string, year: number, enabled = true) {
  return useQuery({
    queryKey: ['bank-book', companyId, bankAccountCode, year],
    queryFn: () => bankApi.book(companyId, bankAccountCode, year),
    enabled: enabled && companyId > 0 && !!bankAccountCode,
  })
}

export function useBankYears(companyId: number, enabled = true) {
  return useQuery({
    queryKey: ['bank-years', companyId],
    queryFn: () => bankApi.years(companyId),
    enabled: enabled && companyId > 0,
  })
}
