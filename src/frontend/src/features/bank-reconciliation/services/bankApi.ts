import apiClient from '../../../shared/services/apiClient'
import type { BankAccount, BankBook } from '../types/bank.types'

export const bankApi = {
  accounts: (clientCompanyId: number) =>
    apiClient.get<BankAccount[]>('/bank/accounts', { params: { clientCompanyId } }).then((r) => r.data),

  book: (clientCompanyId: number, bankAccountCode: string, year: number) =>
    apiClient
      .get<BankBook>('/bank/book', { params: { clientCompanyId, bankAccountCode, year } })
      .then((r) => r.data),

  years: (clientCompanyId: number) =>
    apiClient.get<number[]>('/bank/years', { params: { clientCompanyId } }).then((r) => r.data),
}
