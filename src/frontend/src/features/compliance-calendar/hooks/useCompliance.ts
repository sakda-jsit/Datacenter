import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { assignTask, generateTasks, getDashboard, getTasks, updateStatus } from '../api/complianceApi'
import type { ComplianceTaskStatus } from '../types/compliance.types'

const KEYS = {
  dashboard: (id: number, year: number) => ['compliance', 'dashboard', id, year] as const,
  tasks: (id: number, year: number, month?: number) => ['compliance', 'tasks', id, year, month] as const,
}

export function useComplianceDashboard(clientCompanyId: number, year: number) {
  return useQuery({
    queryKey: KEYS.dashboard(clientCompanyId, year),
    queryFn: () => getDashboard(clientCompanyId, year),
    enabled: clientCompanyId > 0,
  })
}

export function useComplianceTasks(clientCompanyId: number, year: number, month?: number) {
  return useQuery({
    queryKey: KEYS.tasks(clientCompanyId, year, month),
    queryFn: () => getTasks(clientCompanyId, year, month),
    enabled: clientCompanyId > 0,
  })
}

export function useGenerateTasks() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ clientCompanyId, year, month }: { clientCompanyId: number; year: number; month: number }) =>
      generateTasks(clientCompanyId, year, month),
    onSuccess: (_, v) => {
      qc.invalidateQueries({ queryKey: ['compliance', 'tasks', v.clientCompanyId] })
      qc.invalidateQueries({ queryKey: ['compliance', 'dashboard', v.clientCompanyId] })
    },
  })
}

export function useUpdateTaskStatus() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ taskId, status, note }: { taskId: number; status: ComplianceTaskStatus; note?: string }) =>
      updateStatus(taskId, status, note),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['compliance'] }),
  })
}

export function useAssignTask() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ taskId, userId }: { taskId: number; userId: number | null }) =>
      assignTask(taskId, userId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['compliance'] }),
  })
}
