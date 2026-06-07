import { useState } from 'react'
import Button from '../../../../shared/components/ui/Button'
import Card from '../../../../shared/components/ui/Card'
import StateMessage from '../../../../shared/components/ui/StateMessage'
import StatusBadge from '../../../../shared/components/ui/StatusBadge'
import AttachmentUploadModal from '../../components/AttachmentUploadModal'
import {
  useAttachments,
  useDeleteAttachment,
  useDownloadAttachment,
  useSetAttachmentVerification,
} from '../../hooks/useAttachments'
import {
  AttachmentCategory,
  AttachmentVerificationStatus,
  CATEGORY_LABEL,
  VERIFICATION_LABEL,
  type AttachmentDto,
} from '../../types/attachment.types'

interface Props {
  companyId: number
  fiscalYear: number
}

function fmtBytes(n: number) {
  if (n < 1024) return `${n} B`
  if (n < 1024 * 1024) return `${(n / 1024).toLocaleString('th-TH', { maximumFractionDigits: 0 })} KB`
  return `${(n / 1024 / 1024).toLocaleString('th-TH', { maximumFractionDigits: 1 })} MB`
}

function statusTone(s: AttachmentVerificationStatus): 'gray' | 'green' | 'red' | 'yellow' {
  if (s === AttachmentVerificationStatus.Verified) return 'green'
  if (s === AttachmentVerificationStatus.Rejected) return 'red'
  return 'yellow'
}

const CATEGORY_OPTIONS = Object.values(AttachmentCategory).filter(
  (v): v is AttachmentCategory => typeof v === 'number',
)

export default function AttachmentListTab({ companyId, fiscalYear }: Props) {
  const [category, setCategory] = useState<AttachmentCategory | ''>('')
  const [status, setStatus] = useState<AttachmentVerificationStatus | ''>('')
  const [search, setSearch] = useState('')
  const [uploadOpen, setUploadOpen] = useState(false)

  const params = {
    fiscalYear,
    category: category === '' ? undefined : category,
    verificationStatus: status === '' ? undefined : status,
    search: search.trim() || undefined,
  }
  const { data: items, isLoading, isError } = useAttachments(companyId, params)
  const del = useDeleteAttachment(companyId)
  const verify = useSetAttachmentVerification(companyId)
  const download = useDownloadAttachment(companyId)

  if (!companyId) return <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>

  async function handleDelete(a: AttachmentDto) {
    if (!window.confirm(`ลบเอกสาร "${a.title}"? (บันทึก audit trail)`)) return
    await del.mutateAsync(a.id)
  }

  const inputCls = 'rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400'

  return (
    <div>
      <Card className="mb-4 flex flex-wrap items-end justify-between gap-3 px-6 py-4">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ประเภท</label>
            <select value={category} onChange={(e) => setCategory(e.target.value === '' ? '' : Number(e.target.value))} className={inputCls}>
              <option value="">ทั้งหมด</option>
              {CATEGORY_OPTIONS.map((c) => <option key={c} value={c}>{CATEGORY_LABEL[c]}</option>)}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">สถานะตรวจ</label>
            <select value={status} onChange={(e) => setStatus(e.target.value === '' ? '' : Number(e.target.value))} className={inputCls}>
              <option value="">ทั้งหมด</option>
              <option value={AttachmentVerificationStatus.Pending}>รอตรวจ</option>
              <option value={AttachmentVerificationStatus.Verified}>ตรวจแล้ว</option>
              <option value={AttachmentVerificationStatus.Rejected}>ไม่ผ่าน</option>
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-gray-600">ค้นหา</label>
            <input value={search} onChange={(e) => setSearch(e.target.value)} className={`${inputCls} w-48`} placeholder="หัวข้อ/ชื่อไฟล์/อ้างอิง" />
          </div>
        </div>
        <Button type="button" onClick={() => setUploadOpen(true)}>+ แนบเอกสาร</Button>
      </Card>

      {isError && <StateMessage tone="error">เกิดข้อผิดพลาดในการโหลดข้อมูล</StateMessage>}
      {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

      {items && items.length === 0 && (
        <Card><StateMessage centered>ยังไม่มีเอกสารแนบสำหรับปีนี้ — กด "แนบเอกสาร"</StateMessage></Card>
      )}

      {items && items.length > 0 && (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-slate-50">
              <tr className="text-xs text-gray-600">
                <th className="px-4 py-3 text-left font-medium">เอกสาร</th>
                <th className="px-4 py-3 text-left font-medium w-44">ประเภท</th>
                <th className="px-4 py-3 text-left font-medium w-28">วันที่</th>
                <th className="px-4 py-3 text-right font-medium w-24">ขนาด</th>
                <th className="px-4 py-3 text-left font-medium w-28">สถานะ</th>
                <th className="px-4 py-3 text-right font-medium w-56">จัดการ</th>
              </tr>
            </thead>
            <tbody>
              {items.map((a) => (
                <tr key={a.id} className="border-b border-gray-100 align-top hover:bg-slate-50">
                  <td className="px-4 py-2.5 text-gray-800">
                    <div className="font-medium">{a.title}</div>
                    <div className="text-xs text-slate-400">{a.fileName}</div>
                    {(a.recordRef || a.moduleName) && (
                      <div className="text-[11px] text-slate-400">
                        {a.moduleName && <span className="mr-1 rounded bg-slate-100 px-1.5 py-0.5">{a.moduleName}</span>}
                        {a.recordRef}
                      </div>
                    )}
                  </td>
                  <td className="px-4 py-2.5 text-gray-600">{CATEGORY_LABEL[a.category]}</td>
                  <td className="px-4 py-2.5 text-gray-600">{a.documentDate ? a.documentDate.slice(0, 10) : '—'}</td>
                  <td className="px-4 py-2.5 text-right font-mono text-xs text-slate-500">{fmtBytes(a.byteSize)}</td>
                  <td className="px-4 py-2.5">
                    <StatusBadge tone={statusTone(a.verificationStatus)}>{VERIFICATION_LABEL[a.verificationStatus]}</StatusBadge>
                    {a.verifiedBy && <div className="mt-0.5 text-[10px] text-slate-400">โดย {a.verifiedBy}</div>}
                  </td>
                  <td className="px-4 py-2.5 text-right">
                    <Button type="button" variant="ghost" onClick={() => download.mutate({ id: a.id, fileName: a.fileName })} className="px-2 py-1 text-xs text-sky-600">ดาวน์โหลด</Button>
                    {a.verificationStatus !== AttachmentVerificationStatus.Verified && (
                      <Button type="button" variant="ghost" onClick={() => verify.mutate({ id: a.id, status: AttachmentVerificationStatus.Verified })} className="px-2 py-1 text-xs text-green-600">✓ ตรวจแล้ว</Button>
                    )}
                    {a.verificationStatus !== AttachmentVerificationStatus.Rejected && (
                      <Button type="button" variant="ghost" onClick={() => verify.mutate({ id: a.id, status: AttachmentVerificationStatus.Rejected })} className="px-2 py-1 text-xs text-amber-600">✗</Button>
                    )}
                    <Button type="button" variant="ghost" onClick={() => handleDelete(a)} className="px-2 py-1 text-xs text-red-500 hover:text-red-600">ลบ</Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {uploadOpen && (
        <AttachmentUploadModal companyId={companyId} fiscalYear={fiscalYear} onClose={() => setUploadOpen(false)} />
      )}
    </div>
  )
}
