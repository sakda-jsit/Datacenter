export type ComplianceTaskType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11
export type ComplianceTaskStatus = 0 | 1 | 2 | 3

/** ตรงกับ Domain.Enums.ComplianceCycle — API ส่ง enum เป็นตัวเลข */
export type ComplianceCycle = 1 | 2 | 3

export const CYCLE_LABELS: Record<ComplianceCycle, string> = {
  1: 'รายเดือน',
  2: 'ครึ่งปี',
  3: 'รายปี',
}

export const CYCLE_COLORS: Record<ComplianceCycle, string> = {
  1: 'bg-slate-100 text-slate-600',
  2: 'bg-amber-100 text-amber-700',
  3: 'bg-violet-100 text-violet-700',
}

export const TASK_TYPE_LABELS: Record<ComplianceTaskType, string> = {
  1: 'ภ.พ.30 (VAT)',
  2: 'ภ.ง.ด.1',
  3: 'ภ.ง.ด.3',
  4: 'ภ.ง.ด.53',
  5: 'ประกันสังคม',
  6: 'ปิดบัญชี',
  7: 'ภ.ง.ด.51',
  8: 'ภ.ง.ด.50',
  9: 'งบการเงิน + สบช.3',
  10: 'ภ.ง.ด.1ก',
  11: 'กท.20ก',
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
  /** รอบของงาน — รายเดือน / ครึ่งปี / รายปี */
  cycle: ComplianceCycle
  cycleName: string
  /** คำอธิบายงวด เช่น "ม.ค. 2026", "ครึ่งปีแรก 2026", "ปีบัญชี 2026" */
  periodLabel: string
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
  /** จำนวนหลักฐาน (แบบที่ยื่น/ใบเสร็จ) ที่แนบกับงานงวดนี้ */
  evidenceCount: number
  /** งานประเภทนี้ต้องมีหลักฐานก่อนปิดเป็น "เสร็จสิ้น" หรือไม่ */
  requireEvidence: boolean
}

export interface ComplianceTaskTemplateDto {
  taskType: ComplianceTaskType
  taskTypeName: string
  /** รอบของงาน — กำหนดตายตัวตามประเภท แก้ไม่ได้ */
  cycle: ComplianceCycle
  cycleName: string
  enabled: boolean
  /** วันของเดือนเป้าหมาย; null = ใช้ค่าเริ่มต้น, 0 = วันสุดท้ายของเดือน */
  dueDay: number | null
  defaultDueDay: number
  /** ครบกำหนดกี่เดือนหลังสิ้นงวด; null = ใช้ค่าเริ่มต้น */
  dueMonthsAfter: number | null
  defaultDueMonthsAfter: number
  /** คำอธิบายวันครบกำหนดที่ใช้จริง เช่น "150 วันหลังสิ้นรอบบัญชี" */
  dueDescription: string
  /** กำลังใช้กติกานับเป็นจำนวนวัน — ช่องเดือน/วันที่ยังไม่มีผลจนกว่าจะตั้งค่าเอง */
  usesDaysAfterRule: boolean
  /** งวดตัวอย่างของปีปัจจุบัน เช่น "ครึ่งปีแรก 2026" */
  samplePeriodLabel: string
  /** วันครบกำหนดของงวดตัวอย่าง — ใช้ยืนยันว่าค่าที่ตั้งได้วันที่ถูกจริง */
  sampleDueDate: string
  requireEvidence: boolean
  defaultRequireEvidence: boolean
  source: 'default' | 'global' | 'company'
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
