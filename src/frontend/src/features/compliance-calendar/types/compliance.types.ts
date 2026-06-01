export type ComplianceTaskType = 1 | 2 | 3 | 4 | 5 | 6
export type ComplianceTaskStatus = 0 | 1 | 2 | 3

export const TASK_TYPE_LABELS: Record<ComplianceTaskType, string> = {
  1: 'ภ.พ.30 (VAT)',
  2: 'ภ.ง.ด.1',
  3: 'ภ.ง.ด.3',
  4: 'ภ.ง.ด.53',
  5: 'ประกันสังคม',
  6: 'ปิดบัญชี',
}

export const STATUS_LABELS: Record<ComplianceTaskStatus, string> = {
  0: 'รอดำเนินการ',
  1: 'กำลังดำเนินการ',
  2: 'เสร็จสิ้น',
  3: 'เกินกำหนด',
}

export const STATUS_COLORS: Record<ComplianceTaskStatus, string> = {
  0: 'bg-gray-100 text-gray-600',
  1: 'bg-blue-100 text-blue-700',
  2: 'bg-green-100 text-green-700',
  3: 'bg-red-100 text-red-700',
}

export interface ComplianceTaskDto {
  id: number
  clientCompanyId: number
  clientCode: string
  clientName: string
  taskType: ComplianceTaskType
  taskTypeName: string
  year: number
  month: number
  dueDate: string
  status: ComplianceTaskStatus
  statusName: string
  assignedUserId: number | null
  assignedUserName: string | null
  note: string | null
  completedAt: string | null
  completedByUserId: number | null
  completedByUserName: string | null
  isOverdue: boolean
}

export interface MonthSummaryDto {
  month: number
  total: number
  completed: number
  inProgress: number
  pending: number
  overdue: number
}

export interface ComplianceDashboardDto {
  clientCompanyId: number
  clientCode: string
  clientName: string
  year: number
  months: MonthSummaryDto[]
  totalOverdue: number
  upcomingDueSoon: ComplianceTaskDto[]
}
