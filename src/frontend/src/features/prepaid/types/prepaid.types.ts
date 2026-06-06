// ค่าใช้จ่ายจ่ายล่วงหน้า (Prepaid) — req v11 docs/14. API serialize enum เป็น int (ไม่มีในโมดูลนี้)

export interface PrepaidListItem {
  id: number
  code?: string | null
  name: string
  reference?: string | null
  totalAmount: number
  startDate: string
  endDate: string
  isActive: boolean
}

export interface PrepaidExpense {
  id: number
  clientCompanyId: number
  code?: string | null
  name: string
  reference?: string | null
  totalAmount: number
  startDate: string
  endDate: string
  prepaidAccountId: number
  prepaidAccountCode?: string | null
  expenseAccountId: number
  expenseAccountCode?: string | null
  notes?: string | null
  attachmentPath?: string | null
  isActive: boolean
  totalDays: number
}

export interface PrepaidYear {
  year: number
  openingAmortized: number
  charge: number
  closingAmortized: number
  remaining: number
}

export interface PrepaidAsOf {
  openingAmortized: number
  charge: number
  closingAmortized: number
  remaining: number
  fullyAmortized: boolean
}

export interface PrepaidDetail {
  item: PrepaidExpense
  asOf: PrepaidAsOf
  schedule: PrepaidYear[]
}

export interface PrepaidWorkpaperRow {
  id: number
  code?: string | null
  name: string
  reference?: string | null
  totalAmount: number
  startDate: string
  endDate: string
  openingAmortized: number
  chargeInYear: number
  closingAmortized: number
  remaining: number
}

export interface PrepaidGlCompare {
  accountId: number
  accountCode: string
  accountName: string
  scheduleRemaining: number
  glClosing: number
  diff: number
}

export interface PrepaidWorkpaper {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  rows: PrepaidWorkpaperRow[]
  glComparison: PrepaidGlCompare[]
  totalAmount: number
  totalChargeInYear: number
  totalRemaining: number
  hasDifference: boolean
}

export interface PrepaidExpenseInput {
  code?: string | null
  name: string
  reference?: string | null
  totalAmount: number
  startDate: string
  endDate: string
  prepaidAccountId: number
  expenseAccountId: number
  notes?: string | null
  attachmentPath?: string | null
  isActive: boolean
}
