// ตรวจนับเงินสด (Cash Count) — req v11 docs/13. enum ไม่มีในโมดูลนี้

export interface CashCountListItem {
  id: number
  fiscalYear: number
  countDate: string
  reference?: string | null
  cashAccountId: number
  cashAccountCode?: string | null
  countedTotal: number
  isActive: boolean
}

export interface CashCountLine {
  denomination: number
  quantity: number
  amount: number
}

export interface CashCountLineInput {
  denomination: number
  quantity: number
}

export interface CashCount {
  id: number
  clientCompanyId: number
  fiscalYear: number
  countDate: string
  reference?: string | null
  cashAccountId: number
  cashAccountCode?: string | null
  cashAccountName?: string | null
  notes?: string | null
  attachmentPath?: string | null
  isActive: boolean
  countedTotal: number
  lines: CashCountLine[]
}

export interface CashCountInput {
  fiscalYear: number
  countDate: string
  reference?: string | null
  cashAccountId: number
  notes?: string | null
  attachmentPath?: string | null
  isActive: boolean
  lines: CashCountLineInput[]
}

export interface CashCountWorkpaperRow {
  id: number
  countDate: string
  reference?: string | null
  cashAccountId: number
  cashAccountCode?: string | null
  cashAccountName?: string | null
  countedTotal: number
}

export interface CashCountGlCompare {
  accountId: number
  accountCode: string
  accountName: string
  countedTotal: number
  glClosing: number
  diff: number
}

export interface CashCountWorkpaper {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  rows: CashCountWorkpaperRow[]
  glComparison: CashCountGlCompare[]
  totalCounted: number
  hasDifference: boolean
}
