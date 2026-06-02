export interface AuditLogDto {
  id: number
  clientCompanyId: number | null
  clientName: string | null
  userId: number | null
  username: string
  action: string
  entityName: string
  entityId: string | null
  beforeValue: string | null
  afterValue: string | null
  createdAt: string
}

export interface AuditLogParams {
  clientCompanyId?: number
  action?: string
  entityName?: string
  search?: string
  fromDate?: string
  toDate?: string
  pageNumber: number
  pageSize: number
}

export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}
