import apiClient from '../../../shared/services/apiClient'
import type {
  AccountMappingDto,
  BalanceSheetDto,
  EquityChangesDto,
  FsExternalInputDto,
  NotesToFsDto,
  NoteTemplateSectionDto,
  ProfitLossDto,
  StatementTaxonomy,
  UnmappedAccountsResult,
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

  getEquityChanges: (params: { clientCompanyId: number; fiscalYear: number }) =>
    apiClient.get<EquityChangesDto>(`${BASE}/equity-changes`, { params }).then((r) => r.data),

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

  getUnmappedAccounts: (params: { clientCompanyId: number; fiscalYear: number }) =>
    apiClient.get<UnmappedAccountsResult>(`${BASE}/unmapped-accounts`, { params }).then((r) => r.data),

  getTaxonomy: (clientCompanyId: number) =>
    apiClient.get<StatementTaxonomy>(`${BASE}/taxonomy`, { params: { clientCompanyId } }).then((r) => r.data),

  getExternalInputs: (params: { clientCompanyId: number; fiscalYear: number }) =>
    apiClient.get<FsExternalInputDto[]>(`${BASE}/external-inputs`, { params }).then((r) => r.data),

  upsertExternalInput: (data: {
    clientCompanyId: number
    fiscalYear: number
    refCode: string
    amount: number
    note?: string
  }) => apiClient.put(`${BASE}/external-inputs`, data),

  // ── NOTE2 ──
  getNotes: (params: { clientCompanyId: number; fiscalYear: number }) =>
    apiClient.get<NotesToFsDto>(`${BASE}/notes`, { params }).then((r) => r.data),

  // NOTE2 รูปแบบงบ (.xlsx จาก backend ClosedXML) — ดึงเป็น blob (แนบ JWT ผ่าน axios)
  getNotesExcel: (params: { clientCompanyId: number; fiscalYear: number; directorName?: string }) =>
    apiClient.get(`${BASE}/notes/excel`, { params, responseType: 'blob' }).then((r) => r.data as Blob),

  getNoteTemplates: (params: { clientCompanyId: number; fiscalYear: number }) =>
    apiClient.get<NoteTemplateSectionDto[]>(`${BASE}/note-templates`, { params }).then((r) => r.data),

  upsertNoteTemplate: (data: {
    clientCompanyId: number
    effectiveYear: number
    noteKey: string
    title: string
    bodyText: string
    sortOrder: number
  }) => apiClient.put(`${BASE}/note-templates`, data),

  resetNoteTemplate: (data: {
    clientCompanyId: number
    effectiveYear: number
    noteKey: string
  }) => apiClient.post(`${BASE}/note-templates/reset`, data),
}
