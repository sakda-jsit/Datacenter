import apiClient from '../../../shared/services/apiClient'
import type { ReportPackage } from '../types/reportPackage.types'

const BASE = '/report-packages'

export const reportPackageApi = {
  list: (clientCompanyId: number, year = 0) =>
    apiClient.get<ReportPackage[]>(BASE, { params: { clientCompanyId, year } }).then((r) => r.data),

  create: (clientCompanyId: number, fiscalYear: number, title?: string) =>
    apiClient.post<ReportPackage>(BASE, { clientCompanyId, fiscalYear, title }).then((r) => r.data),

  setStatus: (clientCompanyId: number, id: number, targetStatus: number) =>
    apiClient.put<ReportPackage>(`${BASE}/${id}/status`, { clientCompanyId, id, targetStatus }).then((r) => r.data),

  remove: (clientCompanyId: number, id: number) =>
    apiClient.delete(`${BASE}/${id}`, { params: { clientCompanyId } }).then((r) => r.data),
}
