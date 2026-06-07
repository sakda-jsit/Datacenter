import apiClient from '../../../shared/services/apiClient'
import type {
  AttachmentDto,
  AttachmentListParams,
  AttachmentMetadataInput,
  AttachmentUploadInput,
  AttachmentVerificationStatus,
  EvidenceCompleteness,
} from '../types/attachment.types'

export const attachmentApi = {
  list: (clientCompanyId: number, params: AttachmentListParams = {}) =>
    apiClient
      .get<AttachmentDto[]>('/attachments', { params: { clientCompanyId, ...params } })
      .then((r) => r.data),

  completeness: (clientCompanyId: number, fiscalYear: number) =>
    apiClient
      .get<EvidenceCompleteness>('/attachments/completeness', { params: { clientCompanyId, fiscalYear } })
      .then((r) => r.data),

  upload: (clientCompanyId: number, input: AttachmentUploadInput) => {
    const fd = new FormData()
    fd.append('file', input.file)
    fd.append('category', String(input.category))
    fd.append('title', input.title)
    if (input.fiscalYear != null) fd.append('fiscalYear', String(input.fiscalYear))
    if (input.moduleName) fd.append('moduleName', input.moduleName)
    if (input.recordId != null) fd.append('recordId', String(input.recordId))
    if (input.recordRef) fd.append('recordRef', input.recordRef)
    if (input.documentDate) fd.append('documentDate', input.documentDate)
    if (input.note) fd.append('note', input.note)
    return apiClient
      .post<{ id: number }>('/attachments', fd, {
        params: { clientCompanyId },
        headers: { 'Content-Type': undefined },
      })
      .then((r) => r.data)
  },

  update: (id: number, clientCompanyId: number, data: AttachmentMetadataInput) =>
    apiClient.put(`/attachments/${id}`, data, { params: { clientCompanyId } }).then((r) => r.data),

  setVerification: (id: number, clientCompanyId: number, status: AttachmentVerificationStatus, note?: string | null) =>
    apiClient
      .put(`/attachments/${id}/verification`, { status, note: note ?? null }, { params: { clientCompanyId } })
      .then((r) => r.data),

  remove: (id: number, clientCompanyId: number) =>
    apiClient.delete(`/attachments/${id}`, { params: { clientCompanyId } }).then((r) => r.data),

  download: (id: number, clientCompanyId: number) =>
    apiClient
      .get(`/attachments/${id}/download`, { params: { clientCompanyId }, responseType: 'blob' })
      .then((r) => r.data as Blob),
}
