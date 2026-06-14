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

  // รายงานภาษีขาย (vatType=1) / รายงานภาษีซื้อ (vatType=2) เป็น Excel — month 0 = ทั้งปี
  taxReportExcel: (clientCompanyId: number, year: number, vatType: number, month = 0) =>
    apiClient
      .get('/vat/tax-report/excel', { params: { clientCompanyId, year, vatType, month }, responseType: 'blob' })
      .then((r) => r.data as Blob),

  // ไฟล์โอนย้ายข้อมูล ภ.พ.30 (.txt, TIS-620) สำหรับอัปโหลดหน้า e-Filing
  pp30Transfer: (
    clientCompanyId: number, year: number, month: number,
    delimiter = '|', includeHeader = true,
  ) =>
    apiClient
      .get('/vat/pp30-transfer', {
        params: { clientCompanyId, year, month, delimiter, includeHeader },
        responseType: 'blob',
      })
      .then((r) => r.data as Blob),
}
