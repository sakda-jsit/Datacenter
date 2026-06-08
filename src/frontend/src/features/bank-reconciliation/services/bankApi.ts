import apiClient from '../../../shared/services/apiClient'
import type {
  BankAccount, BankBook, BankReconciliation, BankStatementImportListItem, StatementParsePreview,
} from '../types/bank.types'
import type { AdjustmentEntryDto } from '../../adjustments/types/adjustment.types'

const RECON = '/bank-reconciliation'

export const bankApi = {
  accounts: (clientCompanyId: number) =>
    apiClient.get<BankAccount[]>('/bank/accounts', { params: { clientCompanyId } }).then((r) => r.data),

  book: (clientCompanyId: number, bankAccountCode: string, year: number) =>
    apiClient
      .get<BankBook>('/bank/book', { params: { clientCompanyId, bankAccountCode, year } })
      .then((r) => r.data),

  years: (clientCompanyId: number) =>
    apiClient.get<number[]>('/bank/years', { params: { clientCompanyId } }).then((r) => r.data),

  // ── Reconciliation ──
  template: () =>
    apiClient.get(`${RECON}/template`, { responseType: 'blob' }).then((r) => r.data as Blob),

  preview: (clientCompanyId: number, bankAccountId: number, file: File) => {
    const fd = new FormData()
    fd.append('file', file)
    return apiClient
      .post<StatementParsePreview>(`${RECON}/upload`, fd, {
        params: { clientCompanyId, bankAccountId, previewOnly: true },
        headers: { 'Content-Type': undefined },
      })
      .then((r) => r.data)
  },

  upload: (clientCompanyId: number, bankAccountId: number, file: File, openingBalance?: number, closingBalance?: number, note?: string) => {
    const fd = new FormData()
    fd.append('file', file)
    if (openingBalance != null) fd.append('openingBalance', String(openingBalance))
    if (closingBalance != null) fd.append('closingBalance', String(closingBalance))
    if (note) fd.append('note', note)
    return apiClient
      .post<{ id: number }>(`${RECON}/upload`, fd, {
        params: { clientCompanyId, bankAccountId, previewOnly: false },
        headers: { 'Content-Type': undefined },
      })
      .then((r) => r.data)
  },

  imports: (clientCompanyId: number, bankAccountId?: number) =>
    apiClient.get<BankStatementImportListItem[]>(`${RECON}/imports`, { params: { clientCompanyId, bankAccountId } }).then((r) => r.data),

  reconciliation: (clientCompanyId: number, importId: number) =>
    apiClient.get<BankReconciliation>(`${RECON}/${importId}`, { params: { clientCompanyId } }).then((r) => r.data),

  match: (importId: number, clientCompanyId: number, statementLineId: number, bankTransactionId: number) =>
    apiClient.post(`${RECON}/${importId}/match`, { clientCompanyId, statementLineId, bankTransactionId }),

  unmatch: (importId: number, clientCompanyId: number, statementLineId: number) =>
    apiClient.post(`${RECON}/${importId}/unmatch`, { clientCompanyId, statementLineId }),

  deleteImport: (importId: number, clientCompanyId: number) =>
    apiClient.delete(`${RECON}/imports/${importId}`, { params: { clientCompanyId } }),

  generateAdjustment: (input: {
    clientCompanyId: number; importId: number; fiscalYear: number
    statementLineIds: number[]; bankGlAccountId: number; counterpartAccountId: number; entryDate?: string | null
  }) => apiClient.post<AdjustmentEntryDto>(`${RECON}/generate-adjustment`, input).then((r) => r.data),
}
