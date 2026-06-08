export interface WorkTrackerCell {
  taskType: number
  taskTypeName: string
  status: number
  statusName: string
  isOverdue: boolean
  taskId: number
}

export interface WorkTrackerCompanyRow {
  clientCompanyId: number
  clientName: string
  total: number
  completed: number
  open: number
  overdue: number
  cells: WorkTrackerCell[]
}

export interface WorkTrackerAttention {
  taskId: number
  clientCompanyId: number
  clientName: string
  taskType: number
  taskTypeName: string
  dueDate: string
  status: number
  statusName: string
  isOverdue: boolean
  daysToDue: number
}

export interface WorkTrackerOverview {
  year: number
  month: number
  totalTasks: number
  completed: number
  inProgress: number
  pending: number
  overdue: number
  dueSoon: number
  companiesWithOpenWork: number
  companiesWithTasks: number
  totalActiveCompanies: number
  companiesNoTasks: number
  needsAttention: WorkTrackerAttention[]
  companies: WorkTrackerCompanyRow[]
}
