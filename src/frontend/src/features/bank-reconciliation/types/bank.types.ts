export interface BankAccount {
  id: number
  bankAccountCode: string
  bankName: string
  branch?: string
  accountNumber?: string
  glAccountCode?: string
  balanceForward: number
  currentBalance: number
  transactionCount: number
}

export interface BankBookRow {
  id: number
  transactionDate: string
  transactionType?: string
  chequeNo?: string
  counterpartyName?: string
  remark?: string
  deposit: number
  withdrawal: number
  balance: number
}

export interface BankBook {
  clientCompanyId: number
  clientName: string
  bankAccountCode: string
  bankName: string
  accountNumber?: string
  year: number
  openingBalance: number
  rows: BankBookRow[]
  totalDeposit: number
  totalWithdrawal: number
  closingBalance: number
  dataAsOf?: string
}
