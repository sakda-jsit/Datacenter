// enum serialize เป็น integer (ไม่มี JsonStringEnumConverter — ดู api-enum-int-serialization)
// ImportStatus: Pending=0, Running=1, Success=2, Failed=3, Cancelled=4
export type ImportStatus = 0 | 1 | 2 | 3 | 4
// ImportSourceType: ExpressDbf=1, Csv=2, Excel=3
export type ImportSourceType = 1 | 2 | 3

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
  isPosted: boolean
  postedAt?: string
}

export interface PostImportResultDto {
  importBatchId: number
  fiscalYear: number
  accountsUpserted: number
  openingLines: number
  movementLines: number
  message: string
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

// 1=Captured, 2=Partial, 3=Failed (enum serialize เป็น integer)
export type ImportSnapshotStatus = 1 | 2 | 3

export interface ImportSnapshotFileDto {
  tableName: string
  fileName: string
  byteSize: number
  sha256: string
  rowCount?: number
  sourceModifiedAt?: string
}

export interface ImportSnapshotDto {
  id: number
  importBatchId: number
  clientCompanyId: number
  fiscalYear: number
  capturedAt: string
  sourceFolderPath: string
  archiveFileName: string
  archiveByteSize: number
  archiveSha256: string
  fileCount: number
  totalSourceBytes: number
  status: ImportSnapshotStatus
  note?: string
  retainUntil: string
  createdBy: string
  files: ImportSnapshotFileDto[]
}
