import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usersApi, type UserCreateInput, type UserUpdateInput } from '../services/usersApi'

const KEY = ['system-users'] as const

export function useUsers() {
  return useQuery({ queryKey: KEY, queryFn: usersApi.list })
}

export function useCreateUser() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: UserCreateInput) => usersApi.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useUpdateUser() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (v: { id: number; data: UserUpdateInput }) => usersApi.update(v.id, v.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useResetUserPassword() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (v: { id: number; newPassword: string }) => usersApi.resetPassword(v.id, v.newPassword),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}

export function useUnlockUser() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => usersApi.unlock(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  })
}
