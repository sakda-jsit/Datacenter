import apiClient from '../../../shared/services/apiClient'
import type {
  AccountMappingDto,
  BalanceSheetDto,
  FsExternalInputDto,
  ProfitLossDto,
} from '../types/financialStatement.types'

const BASE = '/financial-statement'

export const financialStatementApi = {
  getBalanceSheet: (params: { clientCompanyId: number; fiscalYear: number }) =>
    apiClient.get<BalanceSheetDto>(`${BASE}/balance-sheet`, { params }).then((r) => r.data),

  getProfitLoss: (params: {
    clientCompanyId: number
    fiscalYear: number
    monthFrom?: number
    monthTo?: number
  }) => apiClient.get<ProfitLossDto>(`${BASE}/profit-loss`, { params }).then((r) => r.data),

  getMappings: (clientCompanyId: number) =>
    apiClient.get<AccountMappingDto[]>(`${BASE}/mappings`, {
      params: { clientCompanyId },
    }).then((r) => r.data),

  upsertMapping: (data: {
    clientCompanyId: number
    accountCode: string
    accountName: string
    refCode: string
  }) => apiClient.put(`${BASE}/mappings`, data),

  deleteMapping: (clientCompanyId: number, accountCode: string) =>
    apiClient.delete(`${BASE}/mappings/${clientCompanyId}/${accountCode}`),

  getExternalInputs: (params: { clientCompanyId: number; fiscalYear: number }) =>
    apiClient.get<FsExternalInputDto[]>(`${BASE}/external-inputs`, { params }).then((r) => r.data),

  upsertExternalInput: (data: {
    clientCompanyId: number
    fiscalYear: number
    refCode: string
    amount: number
    note?: string
  }) => apiClient.put(`${BASE}/external-inputs`, data),
}
