import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { taskApi } from '../services/taskApi'
import type { CreateWorkTaskInput, UpdateWorkTaskInput, WorkboardParams } from '../types/task.types'

const keys = {
  list: (companyId: number, status?: number | null, assignee?: number | null) =>
    ['work-tasks', companyId, status ?? null, assignee ?? null] as const,
  board: (p: WorkboardParams) => ['workboard', p] as const,
  users: (companyId: number) => ['assignable-users', companyId] as const,
}

export function useWorkTasks(companyId: number, status?: number | null, assignee?: number | null, enabled = true) {
  return useQuery({
    queryKey: keys.list(companyId, status, assignee),
    queryFn: () => taskApi.list(companyId, status, assignee),
    enabled: enabled && companyId > 0,
  })
}

export function useWorkboard(params: WorkboardParams, enabled = true) {
  return useQuery({
    queryKey: keys.board(params),
    queryFn: () => taskApi.board(params),
    enabled,
  })
}

export function useAssignableUsers(companyId: number) {
  return useQuery({
    queryKey: keys.users(companyId),
    queryFn: () => taskApi.assignableUsers(companyId),
    enabled: companyId > 0,
  })
}

function useInvalidate() {
  const qc = useQueryClient()
  return () => {
    qc.invalidateQueries({ queryKey: ['work-tasks'] })
    qc.invalidateQueries({ queryKey: ['workboard'] })
  }
}

export function useCreateTask() {
  const invalidate = useInvalidate()
  return useMutation({ mutationFn: (input: CreateWorkTaskInput) => taskApi.create(input), onSuccess: invalidate })
}

export function useUpdateTask() {
  const invalidate = useInvalidate()
  return useMutation({ mutationFn: (input: UpdateWorkTaskInput) => taskApi.update(input), onSuccess: invalidate })
}

export function useSetTaskStatus() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ id, status }: { id: number; status: number }) => taskApi.setStatus(id, status),
    onSuccess: invalidate,
  })
}

export function useAssignTask() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ id, userId }: { id: number; userId: number | null }) => taskApi.assign(id, userId),
    onSuccess: invalidate,
  })
}

export function useToggleTaskItem() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ taskId, itemId, isDone }: { taskId: number; itemId: number; isDone: boolean }) =>
      taskApi.toggleItem(taskId, itemId, isDone),
    onSuccess: invalidate,
  })
}

export function useSendReminders() {
  return useMutation({ mutationFn: (daysAhead: number) => taskApi.sendReminders(daysAhead) })
}

export function useDeleteTask() {
  const invalidate = useInvalidate()
  return useMutation({ mutationFn: (id: number) => taskApi.remove(id), onSuccess: invalidate })
}
