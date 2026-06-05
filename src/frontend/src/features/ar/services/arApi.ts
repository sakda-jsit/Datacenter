import apiClient from '../../../shared/services/apiClient'
import type { ArAgingReport, ArInvoice, Customer } from '../types/ar.types'

export const arApi = {
  customers: (clientCompanyId: number, includeInactive = false) =>
    apiClient
      .get<Customer[]>('/ar/customers', { params: { clientCompanyId, includeInactive } })
      .then((r) => r.data),

  invoices: (clientCompanyId: number, year = 0, outstandingOnly = false, customerCode?: string) =>
    apiClient
      .get<ArInvoice[]>('/ar/invoices', { params: { clientCompanyId, year, outstandingOnly, customerCode } })
      .then((r) => r.data),

  aging: (clientCompanyId: number, asOf?: string) =>
    apiClient
      .get<ArAgingReport>('/ar/aging', { params: { clientCompanyId, asOf } })
      .then((r) => r.data),

  years: (clientCompanyId: number) =>
    apiClient.get<number[]>('/ar/years', { params: { clientCompanyId } }).then((r) => r.data),
}
