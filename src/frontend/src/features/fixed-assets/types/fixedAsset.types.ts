// NOTE: API serializes enums as integers (no JsonStringEnumConverter). status/set เป็น number.

/** ตรงกับ Domain.Enums.FixedAssetStatus */
export const AssetStatus = {
  Active: 0,
  Disposed: 1,
  Sold: 2,
  WrittenOff: 3,
} as const

export type FixedAssetStatus = (typeof AssetStatus)[keyof typeof AssetStatus]

export const STATUS_LABEL: Record<number, string> = {
  0: 'ใช้งาน',
  1: 'จำหน่าย/ทิ้ง',
  2: 'ขาย',
  3: 'ตัดจำหน่าย',
}

export const STATUS_OPTIONS: { value: FixedAssetStatus; label: string }[] = [
  { value: 0, label: 'ใช้งาน' },
  { value: 1, label: 'จำหน่าย/ทิ้ง' },
  { value: 2, label: 'ขาย' },
  { value: 3, label: 'ตัดจำหน่าย' },
]

/** ตรงกับ Domain.Enums.DepreciationSet */
export const DepSet = {
  Book: 0,
  Tax: 1,
} as const

export type DepreciationSet = (typeof DepSet)[keyof typeof DepSet]

export const DEP_SET_OPTIONS: { value: DepreciationSet; label: string }[] = [
  { value: 0, label: 'ชุดบัญชี' },
  { value: 1, label: 'ชุดภาษี' },
]

export interface AssetType {
  id: number
  code: string
  name: string
  defaultBookRatePct: number
  defaultTaxRatePct: number
  defaultUsefulLifeYears: number
  isActive: boolean
}

export interface FixedAssetListItem {
  id: number
  assetCode: string
  assetName: string
  assetTypeName?: string
  categoryCode?: string
  acquireDate: string
  cost: number
  bookRatePct: number
  status: number
  isActive: boolean
}

export interface FixedAssetList {
  items: FixedAssetListItem[]
  dataAsOf?: string
}

export interface FixedAsset {
  id: number
  clientCompanyId: number
  assetCode: string
  assetName: string
  assetTypeId?: number
  assetTypeName?: string
  acquireDate: string
  cost: number
  salvageValue: number
  bookRatePct: number
  taxRatePct: number
  accumulatedBroughtForward: number
  broughtForwardYear: number
  assetGroupCode?: string
  categoryCode?: string
  status: number
  disposalDate?: string
  disposalProceeds?: number
  disposalNote?: string
  assetAccountId?: number
  assetAccountCode?: string
  accumDepreciationAccountId: number
  accumDepreciationAccountCode?: string
  depreciationExpenseAccountId: number
  depreciationExpenseAccountCode?: string
  notes?: string
  attachmentPath?: string
  isActive: boolean
  isFromExpress: boolean
}

export interface DepreciationAsOf {
  openingAccumulated: number
  charge: number
  closingAccumulated: number
  netBookValue: number
  fullyDepreciated: boolean
}

export interface DepreciationYear {
  year: number
  openingAccumulated: number
  charge: number
  closingAccumulated: number
  netBookValue: number
}

export interface DisposalResult {
  disposalDate: string
  status: number
  proceeds: number
  netBookValueAtDisposal: number
  gainLoss: number
}

export interface FixedAssetDetail {
  asset: FixedAsset
  fiscalYear: number
  book: DepreciationAsOf
  tax: DepreciationAsOf
  bookSchedule: DepreciationYear[]
  taxSchedule: DepreciationYear[]
  disposal?: DisposalResult
}

export interface FixedAssetWorkpaperRow {
  assetId: number
  assetCode: string
  assetName: string
  assetTypeName?: string
  acquireDate: string
  cost: number
  status: number
  book: DepreciationAsOf
  tax: DepreciationAsOf
  disposal?: DisposalResult
}

export interface FixedAssetTypeSummary {
  assetTypeName: string
  count: number
  cost: number
  bookClosingAccumulated: number
  bookNetBookValue: number
  chargeInYear: number
}

export interface FixedAssetGlCompare {
  accountId: number
  accountCode: string
  accountName: string
  role: string
  scheduleAmount: number
  glAmount: number
  diff: number
}

export interface FixedAssetWorkpaper {
  clientCompanyId: number
  clientCode: string
  clientName: string
  fiscalYear: number
  rows: FixedAssetWorkpaperRow[]
  typeSummary: FixedAssetTypeSummary[]
  glComparison: FixedAssetGlCompare[]
  totalCost: number
  totalBookClosingAccumulated: number
  totalBookNetBookValue: number
  totalBookCharge: number
  totalTaxCharge: number
  hasDifference: boolean
}

/** ฟิลด์ที่แก้ไขได้ (ตรงกับ FixedAssetInput) */
export interface FixedAssetInput {
  assetCode: string
  assetName: string
  assetTypeId?: number | null
  acquireDate: string
  cost: number
  salvageValue: number
  bookRatePct: number
  taxRatePct: number
  accumulatedBroughtForward: number
  broughtForwardYear: number
  assetGroupCode?: string | null
  categoryCode?: string | null
  status: number
  disposalDate?: string | null
  disposalProceeds?: number | null
  disposalNote?: string | null
  assetAccountId?: number | null
  accumDepreciationAccountId: number
  depreciationExpenseAccountId: number
  notes?: string | null
  attachmentPath?: string | null
  isActive: boolean
}

// ── Express import + account mapping ──────────────────────────────────────────

export interface FixedAssetImportResult {
  read: number
  created: number
  updated: number
  unmappedCategories: string[]
  message: string
}

export interface AssetAccountMapping {
  id: number
  clientCompanyId: number
  categoryCode: string
  description?: string
  assetAccountId?: number
  assetAccountCode?: string
  accumDepreciationAccountId?: number
  accumDepreciationAccountCode?: string
  depreciationExpenseAccountId?: number
  depreciationExpenseAccountCode?: string
  assetCount: number
}

export interface AssetAccountMappingInput {
  categoryCode: string
  description?: string | null
  assetAccountId?: number | null
  accumDepreciationAccountId?: number | null
  depreciationExpenseAccountId?: number | null
}
