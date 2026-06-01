import apiClient from '../../../shared/services/apiClient'
import type { GeneralLedgerParams, GeneralLedgerReportDto } from '../types/generalLedger.types'

export const generalLedgerApi = {
  getReport: (params: GeneralLedgerParams) =>
    apiClient
      .get<GeneralLedgerReportDto>('/general-ledger', { params })
      .then((r) => r.data),
}
