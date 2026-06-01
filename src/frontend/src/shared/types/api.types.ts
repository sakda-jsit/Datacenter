export interface PaginatedResult<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface ApiError {
  title: string
  status: number
  errors?: Record<string, string[]>
}
