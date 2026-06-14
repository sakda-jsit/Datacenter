import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  auditorsApi,
  bookkeepersApi,
  type AuditorInput,
  type BookkeeperInput,
} from '../services/auditRegistryApi'

const AK = ['auditors'] as const
const BK = ['bookkeepers'] as const

export function useAuditors() {
  return useQuery({ queryKey: AK, queryFn: auditorsApi.list })
}
export function useSaveAuditor() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (v: { id: number | null; data: AuditorInput }) =>
      v.id ? auditorsApi.update(v.id, v.data) : auditorsApi.create(v.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: AK }),
  })
}
export function useDeleteAuditor() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => auditorsApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: AK }),
  })
}

export function useBookkeepers() {
  return useQuery({ queryKey: BK, queryFn: bookkeepersApi.list })
}
export function useSaveBookkeeper() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (v: { id: number | null; data: BookkeeperInput }) =>
      v.id ? bookkeepersApi.update(v.id, v.data) : bookkeepersApi.create(v.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: BK }),
  })
}
export function useDeleteBookkeeper() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => bookkeepersApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: BK }),
  })
}
