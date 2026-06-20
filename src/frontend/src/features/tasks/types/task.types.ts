// API serializes enums as integers (no string converter) — use numeric enums + send integers.

/** ตรงกับ Domain.Enums.WorkTaskStatus */
export const TaskStatus = { Open: 0, InProgress: 1, Done: 2, Cancelled: 3 } as const
export type WorkTaskStatus = (typeof TaskStatus)[keyof typeof TaskStatus]

export const STATUS_LABEL: Record<number, string> = {
  0: 'เปิด/รอทำ',
  1: 'กำลังทำ',
  2: 'เสร็จสิ้น',
  3: 'ยกเลิก',
}

export const STATUS_TONE: Record<number, 'gray' | 'blue' | 'green' | 'red' | 'yellow'> = {
  0: 'gray',
  1: 'blue',
  2: 'green',
  3: 'gray',
}

export const STATUS_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: 'เปิด/รอทำ' },
  { value: 1, label: 'กำลังทำ' },
  { value: 2, label: 'เสร็จสิ้น' },
  { value: 3, label: 'ยกเลิก' },
]

/** ตรงกับ Domain.Enums.WorkTaskPriority */
export const TaskPriority = { Low: 0, Normal: 1, High: 2 } as const

export const PRIORITY_LABEL: Record<number, string> = { 0: 'ต่ำ', 1: 'ปกติ', 2: 'สูง' }
export const PRIORITY_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: 'ต่ำ' },
  { value: 1, label: 'ปกติ' },
  { value: 2, label: 'สูง' },
]

export interface WorkTaskDto {
  id: number
  clientCompanyId: number
  clientName: string
  title: string
  description?: string | null
  category?: string | null
  status: number
  statusName: string
  priority: number
  priorityName: string
  dueDate?: string | null
  assignedUserId?: number | null
  assignedUserName?: string | null
  completedAt?: string | null
  completedByUserName?: string | null
  isOverdue: boolean
  createdAt: string
  createdBy?: string | null
}

export interface WorkItemDto {
  source: 'Task' | 'Compliance'
  id: number
  clientCompanyId: number
  clientName: string
  title: string
  status: number
  statusName: string
  priority?: number | null
  priorityName?: string | null
  dueDate?: string | null
  assignedUserId?: number | null
  assignedUserName?: string | null
  isOverdue: boolean
  daysToDue?: number | null
}

export interface AssignableUserDto {
  userId: number
  displayName: string
  username: string
  role: number
}

export interface CreateWorkTaskInput {
  clientCompanyId: number
  title: string
  description?: string | null
  category?: string | null
  priority: number
  dueDate?: string | null
  assignedUserId?: number | null
}

export interface UpdateWorkTaskInput extends Omit<CreateWorkTaskInput, 'clientCompanyId'> {
  id: number
}

export interface WorkboardParams {
  assignedUserId?: number | null
  openOnly?: boolean
  dueBefore?: string | null
  includeCompliance?: boolean
}
