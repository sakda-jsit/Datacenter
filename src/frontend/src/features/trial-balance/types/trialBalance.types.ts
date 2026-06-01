export type AccountType = 'Asset' | 'Liability' | 'Equity' | 'Income' | 'Expense'
export type PeriodStatus = 'Open' | 'Closed' | 'Locked'

export interface TrialBalanceRowDto {
  accountId: number
  accountCode: string
  accountName: string
  accountType: AccountType
  level: number
  parentCode?: string
  beginDebit: number
  beginCredit: number
  periodDebit: number
  periodCredit: number
  endDebit: number
  endCredit: number
  beginNet: number
  periodNet: number
  endNet: number
}

export interface TrialBalanceReportDto {
  clientCompanyId: number
  clientCode: string
  clientName: string
  year: number
  monthFrom?: number
  monthTo?: number
  rows: TrialBalanceRowDto[]
  totalBeginDebit: number
  totalBeginCredit: number
  totalPeriodDebit: number
  totalPeriodCredit: number
  totalEndDebit: number
  totalEndCredit: number
}

export interface AccountListDto {
  id: number
  accountCode: string
  accountName: string
  accountName2?: string
  accountType: AccountType
  level: number
  parentCode?: string
  isPostable: boolean
  isActive: boolean
}

export interface PeriodStatusDto {
  year: number
  month: number
  status: PeriodStatus
  closedAt?: string
}

export interface TrialBalanceParams {
  clientCompanyId: number
  year: number
  monthFrom?: number
  monthTo?: number
  includeZeroBalance?: boolean
}
