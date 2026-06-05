import apiClient from '../../../shared/services/apiClient'
import type { VatEntryItem, VatReport } from '../types/vat.types'

export const vatApi = {
  report: (clientCompanyId: number, year: number) =>
    apiClient
      .get<VatReport>('/vat/report', { params: { clientCompanyId, year } })
      .then((r) => r.data),

  years: (clientCompanyId: number) =>
    apiClient.get<number[]>('/vat/years', { params: { clientCompanyId } }).then((r) => r.data),

  entries: (clientCompanyId: number, year: number, month = 0, vatType?: number) =>
    apiClient
      .get<VatEntryItem[]>('/vat', { params: { clientCompanyId, year, month, vatType } })
      .then((r) => r.data),
}
