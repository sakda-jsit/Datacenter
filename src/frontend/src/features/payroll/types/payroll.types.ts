// ── enums (ตรงกับ backend, serialize เป็นเลข) ──────────────────────────────────
export const SalaryType = { Monthly: 1, Daily: 2 } as const
export const EmploymentStatus = { Active: 1, Resigned: 2 } as const
export const SsoMemberStatus = { NotEnrolled: 0, Enrolled: 1, Terminated: 2 } as const
export const EmployeeDocType = {
  IdCardFront: 1,
  SsoEnrollProof: 2,
  SsoTerminateProof: 3,
  Slip: 4,
  Other: 99,
} as const
export const SsoEnrollmentType = { Enroll: 1, Terminate: 2 } as const
export const SsoEnrollmentStatus = { Pending: 0, Submitted: 1 } as const

export const SALARY_TYPE_LABEL: Record<number, string> = { 1: 'รายเดือน', 2: 'รายวัน' }
export const EMPLOYMENT_STATUS_LABEL: Record<number, string> = { 1: 'ปกติ', 2: 'ลาออก' }
export const SSO_STATUS_LABEL: Record<number, string> = { 0: 'ยังไม่แจ้งเข้า', 1: 'แจ้งเข้าแล้ว', 2: 'แจ้งออกแล้ว' }
export const SSO_STATUS_CLASS: Record<number, string> = {
  0: 'bg-amber-50 text-amber-700',
  1: 'bg-green-50 text-green-700',
  2: 'bg-slate-100 text-slate-600',
}
export const DOC_TYPE_LABEL: Record<number, string> = {
  1: 'รูปหน้าบัตรประชาชน',
  2: 'หลักฐานแจ้งเข้า ปกส.',
  3: 'หลักฐานแจ้งออก ปกส.',
  4: 'สลิปเงินเดือน',
  99: 'อื่นๆ',
}
export const ENROLL_TYPE_LABEL: Record<number, string> = { 1: 'แจ้งเข้า', 2: 'แจ้งออก' }
export const ENROLL_STATUS_LABEL: Record<number, string> = { 0: 'รอแจ้ง', 1: 'แจ้งแล้ว' }

// ── types ──────────────────────────────────────────────────────────────────────
export interface EmployeeListItem {
  id: number
  employeeCode: string
  fullName: string
  nationalId: string
  position?: string
  startDate: string
  resignDate?: string | null
  employmentStatus: number
  ssoStatus: number
  baseSalary: number
}

export interface EmployeeDocument {
  id: number
  docType: number
  fileName: string
  contentType: string
  effectiveDate?: string | null
  note?: string | null
  uploadedAt: string
  uploadedBy: string
}

export interface SsoEnrollment {
  id: number
  type: number
  eventDate: string
  submittedDate?: string | null
  status: number
  proofDocumentId?: number | null
  note?: string | null
}

export interface EmployeeDetail {
  id: number
  clientCompanyId: number
  employeeCode: string
  nationalId: string
  prefix?: string | null
  firstName: string
  lastName: string
  birthDate?: string | null
  maritalStatus?: string | null
  nationality?: string | null
  position?: string | null
  department?: string | null
  startDate: string
  resignDate?: string | null
  employmentStatus: number
  salaryType: number
  baseSalary: number
  dailyWage?: number | null
  ssoNumber?: string | null
  ssoHospital?: string | null
  ssoStatus: number
  taxId?: string | null
  note?: string | null
  documents: EmployeeDocument[]
  ssoEnrollments: SsoEnrollment[]
}

export interface PayrollRateConfig {
  id: number
  effectiveFrom: string
  ssoEmployeePct: number
  ssoEmployerPct: number
  ssoWageFloor: number
  ssoWageCap: number
  wcfRatePct: number
  wcfWageCapPerYear: number
  note?: string | null
}

export interface PayrollRateConfigInput {
  effectiveFrom: string
  ssoEmployeePct: number
  ssoEmployerPct: number
  ssoWageFloor: number
  ssoWageCap: number
  wcfRatePct: number
  wcfWageCapPerYear: number
  note?: string
}

// ── งวดเงินเดือน ────────────────────────────────────────────────────────────────
export const PAYROLL_RUN_STATUS_LABEL: Record<number, string> = { 0: 'ร่าง', 1: 'บันทึกแล้ว', 2: 'ปิดงวด' }
export const MONTH_TH = ['', 'มกราคม', 'กุมภาพันธ์', 'มีนาคม', 'เมษายน', 'พฤษภาคม', 'มิถุนายน', 'กรกฎาคม', 'สิงหาคม', 'กันยายน', 'ตุลาคม', 'พฤศจิกายน', 'ธันวาคม']

export interface PayrollRunListItem {
  id: number
  year: number
  month: number
  status: number
  employeeCount: number
  totalGross: number
  totalSsoEmployee: number
  totalTax: number
  totalNet: number
}

export interface PayrollItemRow {
  id: number
  employeeId: number
  employeeCode: string
  employeeName: string
  salaryType: number
  salary: number
  dailyWageDays: number
  dailyWageRate: number
  housingAllowance: number
  foodAllowance: number
  overtime: number
  diligence: number
  bonus: number
  otherIncome: number
  grossIncome: number
  ssoWageBase: number
  ssoEmployee: number
  withholdingTax: number
  absence: number
  advance: number
  otherDeduction: number
  netPay: number
  ssoEmployeeCalc: number
  ssoEmployerCalc: number
  ssoDiff: number
  taxCalc: number
  taxDiff: number
  note?: string | null
}

export interface PayrollRunDetail {
  id: number
  clientCompanyId: number
  year: number
  month: number
  status: number
  note?: string | null
  rateSsoEmployeePct?: number | null
  rateSsoEmployerPct?: number | null
  rateWageFloor?: number | null
  rateWageCap?: number | null
  items: PayrollItemRow[]
  totalGross: number
  totalSsoEmployee: number
  totalSsoEmployer: number
  totalTax: number
  totalNet: number
}

export interface PayrollItemInput {
  id: number
  salary: number
  dailyWageDays: number
  dailyWageRate: number
  housingAllowance: number
  foodAllowance: number
  overtime: number
  diligence: number
  bonus: number
  otherIncome: number
  ssoWageBase: number
  ssoEmployee: number
  withholdingTax: number
  absence: number
  advance: number
  otherDeduction: number
  note?: string | null
}

export interface EmployeeInput {
  employeeCode: string
  nationalId: string
  prefix?: string
  firstName: string
  lastName: string
  birthDate?: string | null
  maritalStatus?: string
  nationality?: string
  position?: string
  department?: string
  startDate: string
  resignDate?: string | null
  employmentStatus: number
  salaryType: number
  baseSalary: number
  dailyWage?: number | null
  ssoNumber?: string
  ssoHospital?: string
  ssoStatus: number
  taxId?: string
  note?: string
}
