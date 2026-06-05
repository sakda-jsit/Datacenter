import apiClient from '../../../shared/services/apiClient'
import type { ApAgingReport, ApInvoice, Supplier } from '../types/ap.types'

export const apApi = {
  suppliers: (clientCompanyId: number, includeInactive = false) =>
    apiClient
      .get<Supplier[]>('/ap/suppliers', { params: { clientCompanyId, includeInactive } })
      .then((r) => r.data),

  invoices: (clientCompanyId: number, year = 0, outstandingOnly = false, supplierCode?: string) =>
    apiClient
      .get<ApInvoice[]>('/ap/invoices', { params: { clientCompanyId, year, outstandingOnly, supplierCode } })
      .then((r) => r.data),

  aging: (clientCompanyId: number, asOf?: string) =>
    apiClient
      .get<ApAgingReport>('/ap/aging', { params: { clientCompanyId, asOf } })
      .then((r) => r.data),

  years: (clientCompanyId: number) =>
    apiClient.get<number[]>('/ap/years', { params: { clientCompanyId } }).then((r) => r.data),
}
