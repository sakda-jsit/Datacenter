export interface ClientListDto {
  id: number
  code: string
  name: string
  taxId: string
  isActive: boolean
}

export interface ClientDetailDto extends ClientListDto {
  legalName: string   // ชื่อทางการ (แก้ได้/ใช้ออกงบ); name = ชื่อจาก Express (อ้างอิง)
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
  legalName: string
  taxId: string
  branchCode: string
  address?: string
  fiscalYearStartMonth: number
}
