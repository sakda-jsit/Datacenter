import { useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import { useUploadAttachment } from '../hooks/useAttachments'
import { AttachmentCategory, CATEGORY_LABEL } from '../types/attachment.types'

interface Props {
  companyId: number
  fiscalYear: number
  /** หมวดเริ่มต้น (เช่น เปิดจาก checklist ของหมวดนั้น) */
  defaultCategory?: AttachmentCategory
  /** ผูกกับระเบียนของโมดูล (ถ้าเปิดจากหน้ารายละเอียดโมดูล) */
  moduleName?: string | null
  recordId?: number | null
  recordRef?: string | null
  onClose: () => void
}

function todayIso() {
  return new Date().toISOString().slice(0, 10)
}

const CATEGORY_OPTIONS = Object.values(AttachmentCategory).filter(
  (v): v is AttachmentCategory => typeof v === 'number',
)

export default function AttachmentUploadModal({
  companyId, fiscalYear, defaultCategory, moduleName, recordId, recordRef, onClose,
}: Props) {
  const upload = useUploadAttachment(companyId)
  const [file, setFile] = useState<File | null>(null)
  const [category, setCategory] = useState<AttachmentCategory>(defaultCategory ?? AttachmentCategory.Other)
  const [title, setTitle] = useState('')
  const [ref, setRef] = useState(recordRef ?? '')
  const [documentDate, setDocumentDate] = useState('')
  const [note, setNote] = useState('')
  const [error, setError] = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (!file) return setError('กรุณาเลือกไฟล์')
    if (file.size > 15 * 1024 * 1024) return setError('ไฟล์ใหญ่เกิน 15 MB')
    const finalTitle = title.trim() || file.name
    try {
      await upload.mutateAsync({
        file,
        category,
        title: finalTitle,
        fiscalYear,
        moduleName: moduleName ?? null,
        recordId: recordId ?? null,
        recordRef: ref.trim() || null,
        documentDate: documentDate || null,
        note: note.trim() || null,
      })
      onClose()
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'อัปโหลดไม่สำเร็จ')
    }
  }

  const inputCls = 'w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400'

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <h2 className="text-lg font-bold text-slate-800">แนบเอกสาร/หลักฐาน</h2>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>
        <form onSubmit={handleSubmit} className="px-6 py-4">
          <div className="grid grid-cols-1 gap-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">ไฟล์ * (สูงสุด 15 MB)</label>
              <input
                type="file"
                onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                className="block w-full text-sm text-slate-600 file:mr-3 file:rounded file:border-0 file:bg-sky-50 file:px-3 file:py-2 file:text-sm file:font-medium file:text-sky-700 hover:file:bg-sky-100"
              />
              {file && <p className="mt-1 text-xs text-slate-400">{file.name} · {(file.size / 1024).toLocaleString('th-TH', { maximumFractionDigits: 0 })} KB</p>}
            </div>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-xs font-medium text-gray-600">ประเภทเอกสาร *</label>
                <select value={category} onChange={(e) => setCategory(Number(e.target.value))} className={inputCls}>
                  {CATEGORY_OPTIONS.map((c) => <option key={c} value={c}>{CATEGORY_LABEL[c]}</option>)}
                </select>
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium text-gray-600">วันที่เอกสาร</label>
                <input type="date" value={documentDate} onChange={(e) => setDocumentDate(e.target.value)} className={inputCls} max={todayIso()} />
              </div>
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">หัวข้อ/คำอธิบาย</label>
              <input value={title} onChange={(e) => setTitle(e.target.value)} className={inputCls} placeholder="เว้นว่าง = ใช้ชื่อไฟล์" />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">เลขที่อ้างอิง</label>
              <input value={ref} onChange={(e) => setRef(e.target.value)} className={inputCls} placeholder="เลขที่เอกสาร/สัญญา" />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-gray-600">หมายเหตุ</label>
              <input value={note} onChange={(e) => setNote(e.target.value)} className={inputCls} />
            </div>
          </div>

          <p className="mt-3 text-xs text-slate-400">แนบเป็นหลักฐานปีบัญชี <b>{fiscalYear}</b> · เก็บถาวร ≥ 10 ปี · บันทึก audit trail</p>
          {error && <p className="mt-3 rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}

          <div className="mt-5 flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={onClose}>ยกเลิก</Button>
            <Button type="submit" disabled={upload.isPending}>{upload.isPending ? 'กำลังอัปโหลด...' : 'อัปโหลด'}</Button>
          </div>
        </form>
      </div>
    </div>
  )
}
