// คลังเอกสารแนบ/หลักฐาน (Attachment / Evidence) — docs/18 §17-20.
// API serialize enum เป็น int → ใช้ numeric enum + ส่งตัวเลข

export enum AttachmentCategory {
  Other = 0,
  BankStatement = 1,
  TaxInvoice = 2,
  WhtCertificate = 3,
  RevenueFiling = 4,
  SocialSecurityFiling = 5,
  FixedAssetDocument = 6,
  PrepaidDocument = 7,
  ContractDocument = 8,
  FinancialStatement = 9,
  ImportFile = 10,
  BankConfirmation = 11,
}

export const CATEGORY_LABEL: Record<AttachmentCategory, string> = {
  [AttachmentCategory.Other]: 'อื่น ๆ',
  [AttachmentCategory.BankStatement]: 'Bank statement',
  [AttachmentCategory.TaxInvoice]: 'ใบกำกับภาษี',
  [AttachmentCategory.WhtCertificate]: 'หนังสือรับรองหัก ณ ที่จ่าย',
  [AttachmentCategory.RevenueFiling]: 'แบบ/ใบเสร็จสรรพากร',
  [AttachmentCategory.SocialSecurityFiling]: 'เอกสารประกันสังคม',
  [AttachmentCategory.FixedAssetDocument]: 'เอกสารสินทรัพย์ถาวร',
  [AttachmentCategory.PrepaidDocument]: 'เอกสารค่าใช้จ่ายจ่ายล่วงหน้า',
  [AttachmentCategory.ContractDocument]: 'สัญญาเช่าซื้อ/เงินกู้',
  [AttachmentCategory.FinancialStatement]: 'งบการเงิน',
  [AttachmentCategory.ImportFile]: 'ไฟล์นำเข้า Express',
  [AttachmentCategory.BankConfirmation]: 'หนังสือยืนยันยอดธนาคาร',
}

export enum AttachmentVerificationStatus {
  Pending = 0,
  Verified = 1,
  Rejected = 2,
}

export const VERIFICATION_LABEL: Record<AttachmentVerificationStatus, string> = {
  [AttachmentVerificationStatus.Pending]: 'รอตรวจ',
  [AttachmentVerificationStatus.Verified]: 'ตรวจแล้ว',
  [AttachmentVerificationStatus.Rejected]: 'ไม่ผ่าน',
}

export interface AttachmentDto {
  id: number
  clientCompanyId: number
  category: AttachmentCategory
  fiscalYear?: number | null
  moduleName?: string | null
  recordId?: number | null
  recordRef?: string | null
  title: string
  fileName: string
  contentType: string
  byteSize: number
  sha256: string
  documentDate?: string | null
  verificationStatus: AttachmentVerificationStatus
  verifiedBy?: string | null
  verifiedAt?: string | null
  note?: string | null
  createdBy: string
  createdAt: string
}

export interface EvidenceCompletenessItem {
  category: AttachmentCategory
  label: string
  required: boolean
  count: number
  verifiedCount: number
  present: boolean
}

export interface EvidenceCompleteness {
  fiscalYear: number
  isComplete: boolean
  totalAttachments: number
  requiredMissingCount: number
  items: EvidenceCompletenessItem[]
}

export interface AttachmentListParams {
  fiscalYear?: number
  category?: AttachmentCategory
  moduleName?: string
  recordId?: number
  verificationStatus?: AttachmentVerificationStatus
  search?: string
}

export interface AttachmentUploadInput {
  file: File
  category: AttachmentCategory
  title: string
  fiscalYear?: number | null
  moduleName?: string | null
  recordId?: number | null
  recordRef?: string | null
  documentDate?: string | null
  note?: string | null
}

export interface AttachmentMetadataInput {
  category: AttachmentCategory
  fiscalYear?: number | null
  recordRef?: string | null
  title: string
  documentDate?: string | null
  note?: string | null
}
