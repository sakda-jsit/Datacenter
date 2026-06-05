// API serializes enum as int. status: 0=Draft,1=Review,2=Final,3=Locked

export const RP_STATUS = { Draft: 0, Review: 1, Final: 2, Locked: 3 } as const

export const RP_STATUS_LABEL: Record<number, string> = {
  0: 'ร่าง',
  1: 'รอตรวจ',
  2: 'อนุมัติ (Final)',
  3: 'ล็อก (ยื่นแล้ว)',
}

export const RP_STATUS_CLASS: Record<number, string> = {
  0: 'bg-gray-100 text-gray-600',
  1: 'bg-amber-100 text-amber-700',
  2: 'bg-sky-100 text-sky-700',
  3: 'bg-red-100 text-red-700',
}

export interface ReportPackage {
  id: number
  clientCompanyId: number
  fiscalYear: number
  version: number
  status: number
  title?: string
  note?: string
  snapshotCompanyName?: string
  snapshotTaxId?: string
  snapshotBranchCode?: string
  snapshotAddress?: string
  totalAssets?: number
  totalLiabilities?: number
  totalEquity?: number
  totalRevenue?: number
  netProfit?: number
  finalizedAt?: string
  finalizedBy?: string
  lockedAt?: string
  lockedBy?: string
  createdAt: string
  createdBy: string
}
