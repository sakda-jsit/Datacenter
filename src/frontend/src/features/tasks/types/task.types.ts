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

/** ตรงกับ Domain.Enums.WorkTaskRecurrence */
export const TaskRecurrence = { None: 0, Daily: 1, Weekly: 2, Monthly: 3, Yearly: 4 } as const

export const RECURRENCE_LABEL: Record<number, string> = {
  0: 'ไม่ซ้ำ', 1: 'รายวัน', 2: 'รายสัปดาห์', 3: 'รายเดือน', 4: 'รายปี',
}
export const RECURRENCE_OPTIONS: { value: number; label: string }[] = [
  { value: 0, label: 'ไม่ซ้ำ (ครั้งเดียว)' },
  { value: 1, label: 'รายวัน' },
  { value: 2, label: 'รายสัปดาห์' },
  { value: 3, label: 'รายเดือน' },
  { value: 4, label: 'รายปี' },
]

export interface WorkTaskItemDto {
  id: number
  text: string
  isDone: boolean
  sortOrder: number
}

export interface WorkTaskItemInput {
  text: string
  isDone: boolean
}

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
  recurrenceType: number
  recurrenceName: string
  recurrenceInterval: number
  items: WorkTaskItemDto[]
  doneCount: number
  totalCount: number
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

/** ภาระงานของผู้รับผิดชอบ 1 คน (userId = null → งานที่ยังไม่มอบหมาย) */
export interface UserWorkloadDto {
  userId: number | null
  displayName: string
  openCount: number
  overdueCount: number
  dueSoonCount: number
  noDueDateCount: number
  companyCount: number
  earliestDueDate: string | null
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
  recurrenceType?: number
  recurrenceInterval?: number
  items?: WorkTaskItemInput[]
}

export interface UpdateWorkTaskInput extends Omit<CreateWorkTaskInput, 'clientCompanyId'> {
  id: number
}

export interface TaskReminderResult {
  sent: number
  skipped: number
  failed: number
  messages: string[]
}

export interface WorkboardParams {
  assignedUserId?: number | null
  openOnly?: boolean
  dueBefore?: string | null
  includeCompliance?: boolean
}
