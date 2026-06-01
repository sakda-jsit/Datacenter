export interface GeneralLedgerLineDto {
  journalEntryId: number
  documentNo: string
  journalDate: string
  description: string
  sourceModule: string
  debitAmount: number
  creditAmount: number
  runningBalance: number
}

export interface GeneralLedgerAccountDto {
  accountId: number
  accountCode: string
  accountName: string
  openingBalance: number
  totalDebit: number
  totalCredit: number
  closingBalance: number
  lines: GeneralLedgerLineDto[]
}

export interface GeneralLedgerReportDto {
  clientCompanyId: number
  clientCode: string
  clientName: string
  year: number
  monthFrom?: number
  monthTo?: number
  accounts: GeneralLedgerAccountDto[]
}

export interface GeneralLedgerParams {
  clientCompanyId: number
  year: number
  monthFrom?: number
  monthTo?: number
  accountId?: number
}
