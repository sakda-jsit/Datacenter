import apiClient from '../../../shared/services/apiClient'
import type { ComplianceDashboardDto, ComplianceTaskDto, ComplianceTaskStatus } from '../types/compliance.types'

export async function getDashboard(clientCompanyId: number, year: number): Promise<ComplianceDashboardDto> {
  const { data } = await apiClient.get('/compliance-calendar/dashboard', { params: { clientCompanyId, year } })
  return data
}

export async function getTasks(
  clientCompanyId: number,
  year: number,
  month?: number,
  status?: ComplianceTaskStatus,
): Promise<ComplianceTaskDto[]> {
  const { data } = await apiClient.get('/compliance-calendar/tasks', {
    params: { clientCompanyId, year, month, status },
  })
  return data
}

export async function generateTasks(clientCompanyId: number, year: number, month: number): Promise<{ created: number }> {
  const { data } = await apiClient.post('/compliance-calendar/generate', { clientCompanyId, year, month })
  return data
}

export async function updateStatus(
  taskId: number,
  status: ComplianceTaskStatus,
  note?: string,
): Promise<void> {
  await apiClient.patch(`/compliance-calendar/tasks/${taskId}/status`, { status, note })
}

export async function assignTask(taskId: number, userId: number | null): Promise<void> {
  await apiClient.patch(`/compliance-calendar/tasks/${taskId}/assign`, { userId })
}
