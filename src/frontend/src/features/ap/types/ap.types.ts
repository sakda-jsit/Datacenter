export interface Supplier {
  id: number
  supplierCode: string
  prefix?: string
  name: string
  taxId?: string
  address?: string
  phone?: string
  contact?: string
  email?: string
  paymentTermDays: number
  paymentCondition?: string
  glAccountCode?: string
  isActive: boolean
  outstandingAmount: number
  openInvoiceCount: number
}

export interface ApInvoice {
  id: number
  documentNo: string
  documentDate: string
  dueDate?: string
  supplierCode: string
  supplierName?: string
  amount: number
  vatAmount: number
  netAmount: number
  paidAmount: number
  outstandingAmount: number
  isCompleted: boolean
  reference?: string
}

export interface ApAgingRow {
  supplierCode: string
  supplierName: string
  notDue: number
  days1To30: number
  days31To60: number
  days61To90: number
  days90Plus: number
  total: number
}

export interface ApAgingReport {
  clientCompanyId: number
  clientName: string
  asOfDate: string
  rows: ApAgingRow[]
  totalNotDue: number
  totalDays1To30: number
  totalDays31To60: number
  totalDays61To90: number
  totalDays90Plus: number
  grandTotal: number
  dataAsOf?: string
}
