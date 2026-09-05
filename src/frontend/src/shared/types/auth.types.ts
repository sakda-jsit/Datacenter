export interface AuthUser {
  userId: number
  username: string
  displayName: string
  role: 'Admin' | 'Maker' | 'Checker' | 'Supervisor'
  token: string
  /** ใช้ต่ออายุการเข้าใช้งานเมื่อ token หมดอายุ (apiClient จัดการให้อัตโนมัติ) */
  refreshToken: string
  /** เวลาหมดอายุของ access token (ISO, UTC) */
  expiresAt: string
  /** true = ผู้ดูแลตั้งรหัสให้ ต้องเปลี่ยนรหัสก่อนใช้งานส่วนอื่น */
  mustChangePassword: boolean
  /**
   * บริษัทที่ผู้ใช้นี้ "ดูแล" = ทำรายการได้ (ดูข้อมูลได้ทุกบริษัทอยู่แล้ว)
   * null = Admin ดูแลได้ทุกบริษัท
   */
  ownedCompanyIds: number[] | null
}
