export interface FsLineAccountDto {
  accountCode: string
  accountName: string
  netBalance: number
}

export interface FsLineDto {
  refCode: string
  lineName: string
  section: string
  sortOrder: number
  amount: number
  accounts: FsLineAccountDto[]
}

export interface BalanceSheetDto {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  assets: FsLineDto[]
  liabilities: FsLineDto[]
  equity: FsLineDto[]
  totalAssets: number
  totalLiabilities: number
  totalEquity: number
  totalLiabilitiesAndEquity: number
  balanceDifference: number
}

export interface ProfitLossDto {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  monthFrom?: number
  monthTo?: number
  incomeLines: FsLineDto[]
  costOfGoods: FsLineDto
  expenseLines: FsLineDto[]
  financeCost: FsLineDto
  incomeTax: FsLineDto
  totalIncome: number
  totalExpenses: number
  grossProfit: number
  profitBeforeFinance: number
  profitBeforeTax: number
  netProfit: number
}

export interface AccountMappingDto {
  accountCode: string
  accountName: string
  refCode: string
  lineName: string
  section: string
}
