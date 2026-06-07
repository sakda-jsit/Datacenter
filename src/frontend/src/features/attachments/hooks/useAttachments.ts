import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { attachmentApi } from '../services/attachmentApi'
import type {
  AttachmentListParams,
  AttachmentMetadataInput,
  AttachmentUploadInput,
  AttachmentVerificationStatus,
} from '../types/attachment.types'

const keys = {
  list: (companyId: number, params: AttachmentListParams) => ['attachments', companyId, params] as const,
  completeness: (companyId: number, year: number) => ['attachments-completeness', companyId, year] as const,
}

export function useAttachments(companyId: number, params: AttachmentListParams = {}, enabled = true) {
  return useQuery({
    queryKey: keys.list(companyId, params),
    queryFn: () => attachmentApi.list(companyId, params),
    enabled: enabled && companyId > 0,
  })
}

export function useEvidenceCompleteness(companyId: number, year: number, enabled = true) {
  return useQuery({
    queryKey: keys.completeness(companyId, year),
    queryFn: () => attachmentApi.completeness(companyId, year),
    enabled: enabled && companyId > 0,
  })
}

function useInvalidate(companyId: number) {
  const qc = useQueryClient()
  return () => {
    qc.invalidateQueries({ queryKey: ['attachments', companyId] })
    qc.invalidateQueries({ queryKey: ['attachments-completeness', companyId] })
  }
}

export function useUploadAttachment(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (input: AttachmentUploadInput) => attachmentApi.upload(companyId, input),
    onSuccess: invalidate,
  })
}

export function useUpdateAttachment(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: AttachmentMetadataInput }) =>
      attachmentApi.update(id, companyId, data),
    onSuccess: invalidate,
  })
}

export function useSetAttachmentVerification(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: ({ id, status, note }: { id: number; status: AttachmentVerificationStatus; note?: string | null }) =>
      attachmentApi.setVerification(id, companyId, status, note),
    onSuccess: invalidate,
  })
}

export function useDeleteAttachment(companyId: number) {
  const invalidate = useInvalidate(companyId)
  return useMutation({
    mutationFn: (id: number) => attachmentApi.remove(id, companyId),
    onSuccess: invalidate,
  })
}

export function useDownloadAttachment(companyId: number) {
  return useMutation({
    mutationFn: ({ id, fileName }: { id: number; fileName: string }) =>
      attachmentApi.download(id, companyId).then((blob) => {
        const url = URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        a.download = fileName
        document.body.appendChild(a)
        a.click()
        a.remove()
        URL.revokeObjectURL(url)
      }),
  })
}
