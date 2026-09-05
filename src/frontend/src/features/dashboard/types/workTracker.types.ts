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
  /** งวดของงาน เช่น "ส.ค. 2026", "ปีบัญชี 2026" — งานรอบยาวครบกำหนดคนละเดือนกับงวด */
  periodLabel: string
}

/** คอลัมน์ประเภทงานในตาราง — มาจากงานที่มีจริงในงวดนั้น ไม่ใช่รายการตายตัว */
export interface WorkTrackerColumn {
  taskType: number
  shortName: string
  taskTypeName: string
}

export interface WorkTrackerOverview {
  year: number
  month: number
  /** true = ตัวเลขนับเฉพาะบริษัทที่ผู้ใช้ดูแล (ไม่ใช่ Admin) */
  scopedToOwnedCompanies: boolean
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
  columns: WorkTrackerColumn[]
}
