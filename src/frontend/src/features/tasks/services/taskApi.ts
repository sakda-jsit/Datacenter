import apiClient from '../../../shared/services/apiClient'
import type {
  AssignableUserDto,
  CreateWorkTaskInput,
  TaskReminderResult,
  UpdateWorkTaskInput,
  WorkboardParams,
  WorkItemDto,
  WorkTaskDto,
} from '../types/task.types'

export const taskApi = {
  list: (clientCompanyId: number, status?: number | null, assignedUserId?: number | null) =>
    apiClient
      .get<WorkTaskDto[]>('/work-tasks', { params: { clientCompanyId, status, assignedUserId } })
      .then((r) => r.data),

  board: (params: WorkboardParams) =>
    apiClient.get<WorkItemDto[]>('/work-tasks/board', { params }).then((r) => r.data),

  assignableUsers: (clientCompanyId: number) =>
    apiClient
      .get<AssignableUserDto[]>('/work-tasks/assignable-users', { params: { clientCompanyId } })
      .then((r) => r.data),

  create: (input: CreateWorkTaskInput) =>
    apiClient.post<WorkTaskDto>('/work-tasks', input).then((r) => r.data),

  update: (input: UpdateWorkTaskInput) =>
    apiClient.put<WorkTaskDto>(`/work-tasks/${input.id}`, input).then((r) => r.data),

  setStatus: (id: number, status: number) =>
    apiClient.patch<WorkTaskDto>(`/work-tasks/${id}/status`, { status }).then((r) => r.data),

  assign: (id: number, userId: number | null) =>
    apiClient.patch<WorkTaskDto>(`/work-tasks/${id}/assign`, { userId }).then((r) => r.data),

  toggleItem: (taskId: number, itemId: number, isDone: boolean) =>
    apiClient.patch<WorkTaskDto>(`/work-tasks/${taskId}/items/${itemId}`, { isDone }).then((r) => r.data),

  sendReminders: (daysAhead = 3) =>
    apiClient.post<TaskReminderResult>(`/work-tasks/send-reminders`, null, { params: { daysAhead } }).then((r) => r.data),

  remove: (id: number) => apiClient.delete(`/work-tasks/${id}`).then((r) => r.data),
}
