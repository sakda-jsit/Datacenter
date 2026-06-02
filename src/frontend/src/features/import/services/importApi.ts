import apiClient from '../../../shared/services/apiClient'
import type { PaginatedResult } from '../../../shared/types/api.types'
import type {
  ImportBatchListDto,
  ImportValidationSummaryDto,
  PostImportResultDto,
  StartExpressImportRequest,
} from '../types/import.types'

export const importApi = {
  getHistory: (params: {
    clientCompanyId?: number
    fiscalYear?: number
    pageNumber?: number
    pageSize?: number
  }) =>
    apiClient
      .get<PaginatedResult<ImportBatchListDto>>('/import', { params })
      .then((r) => r.data),

  getValidation: (id: number) =>
    apiClient
      .get<ImportValidationSummaryDto>(`/import/${id}/validation`)
      .then((r) => r.data),

  startExpressImport: (data: StartExpressImportRequest) =>
    apiClient.post<{ id: number }>('/import/express', data).then((r) => r.data),

  postBatch: (id: number) =>
    apiClient.post<PostImportResultDto>(`/import/${id}/post`).then((r) => r.data),

  deleteBatch: (id: number) =>
    apiClient.delete(`/import/${id}`).then((r) => r.data),
}
