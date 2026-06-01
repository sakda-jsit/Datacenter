export type ImportStatus = 'Pending' | 'Running' | 'Success' | 'Failed' | 'Cancelled'
export type ImportSourceType = 'ExpressDbf' | 'Csv' | 'Excel'

export interface ImportBatchListDto {
  id: number
  clientCompanyId: number
  clientCode: string
  clientName: string
  sourceType: ImportSourceType
  importType: string
  fiscalYear: number
  status: ImportStatus
  totalRows: number
  successRows: number
  errorRows: number
  message?: string
  createdAt: string
  createdBy: string
  finishedAt?: string
}

export interface ImportBatchDetailDto {
  id: number
  rowNumber: number
  accountCode?: string
  isValid: boolean
  errorMessage?: string
  rawData: string
}

export interface ImportValidationSummaryDto {
  importBatchId: number
  totalRows: number
  validRows: number
  invalidRows: number
  errors: ImportBatchDetailDto[]
}

export interface StartExpressImportRequest {
  clientCompanyId: number
  fiscalYear: number
}
