export type PeriodStatus = 'Open' | 'Closed' | 'Locked'

// Backend ส่ง enum เป็นตัวเลข (Open=0, Closed=1, Locked=2) ผ่าน JSON
export type PeriodStatusValue = 0 | 1 | 2

export interface ClosingPeriodMonthDto {
  year: number
  month: number
  status: PeriodStatusValue
  statusName: string
  closedAt: string | null
  closedByUserId: number | null
  closedByName: string | null
  beginDate: string | null
  endDate: string | null
  sourceLocked: boolean
}

export interface ClosingPeriodOverviewDto {
  clientCompanyId: number
  clientCode: string
  clientName: string
  year: number
  isDefined: boolean
  months: ClosingPeriodMonthDto[]
}

export interface ClosingValidationItemDto {
  code: string
  label: string
  severity: 'Error' | 'Warning' | 'Info'
  passed: boolean
  detail: string | null
}

export interface ClosingValidationDto {
  clientCompanyId: number
  year: number
  month: number
  currentStatus: PeriodStatusValue
  canClose: boolean
  items: ClosingValidationItemDto[]
}
