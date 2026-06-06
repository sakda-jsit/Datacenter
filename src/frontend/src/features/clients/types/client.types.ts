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
  ssoAccountNo?: string | null
  ssoBranchCode?: string | null
  phone?: string | null
  postalCode?: string | null
}

export interface CreateClientRequest {
  code: string
  name: string
  taxId: string
  branchCode: string
  address?: string
  fiscalYearStartMonth: number
  // ปกส. (ใช้ตอนแก้ไข — สำหรับ สปส.1-10)
  ssoAccountNo?: string
  ssoBranchCode?: string
  phone?: string
  postalCode?: string
}

export interface UpdateClientRequest {
  legalName: string
  taxId: string
  branchCode: string
  address?: string
  fiscalYearStartMonth: number
  ssoAccountNo?: string
  ssoBranchCode?: string
  phone?: string
  postalCode?: string
}
