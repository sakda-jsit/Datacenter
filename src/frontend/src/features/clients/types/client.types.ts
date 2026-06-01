export interface ClientListDto {
  id: number
  code: string
  name: string
  taxId: string
  isActive: boolean
}

export interface ClientDetailDto extends ClientListDto {
  branchCode: string
  address?: string
  fiscalYearStartMonth: number
}

export interface CreateClientRequest {
  code: string
  name: string
  taxId: string
  branchCode: string
  address?: string
  fiscalYearStartMonth: number
}

export interface UpdateClientRequest {
  name: string
  taxId: string
  branchCode: string
  address?: string
  fiscalYearStartMonth: number
}
