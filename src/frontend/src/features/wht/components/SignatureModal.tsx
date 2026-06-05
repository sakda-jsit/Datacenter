import { useRef, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useWhtSignature, useUploadSignature, useDeleteSignature } from '../hooks/useWht'

interface Props {
  companyId: number
  onClose: () => void
}

export default function SignatureModal({ companyId, onClose }: Props) {
  const { data, isLoading } = useWhtSignature(companyId)
  const upload = useUploadSignature(companyId)
  const remove = useDeleteSignature(companyId)
  const fileRef = useRef<HTMLInputElement>(null)
  const [error, setError] = useState('')

  function pickFile() {
    fileRef.current?.click()
  }

  async function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    setError('')
    const file = e.target.files?.[0]
    e.target.value = '' // ให้เลือกไฟล์เดิมซ้ำได้
    if (!file) return
    if (!/^image\/(png|jpe?g)$/.test(file.type)) {
      setError('รองรับเฉพาะไฟล์รูป PNG หรือ JPG')
      return
    }
    if (file.size > 2 * 1024 * 1024) {
      setError('ไฟล์ใหญ่เกิน 2 MB')
      return
    }
    try {
      await upload.mutateAsync(file)
    } catch (err) {
      const msg = (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
      setError(msg?.detail ?? msg?.title ?? 'อัปโหลดไม่สำเร็จ')
    }
  }

  async function onDelete() {
    setError('')
    try {
      await remove.mutateAsync()
    } catch {
      setError('ลบไม่สำเร็จ')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-12 w-full max-w-md rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <h2 className="text-lg font-bold text-slate-800">ลายเซ็นผู้มีหน้าที่หักภาษี</h2>
            <p className="text-xs text-gray-500">ใช้แนบเหนือเส้นลงชื่อในหนังสือรับรองหัก ณ ที่จ่าย (PNG/JPG ≤ 2 MB · พื้นหลังโปร่งใสจะสวยที่สุด)</p>
          </div>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="space-y-4 px-6 py-5">
          {isLoading ? (
            <StateMessage>กำลังโหลด...</StateMessage>
          ) : (
            <div className="flex min-h-[120px] items-center justify-center rounded-lg border border-dashed border-gray-300 bg-slate-50 p-4">
              {data?.hasSignature && data.dataUrl ? (
                <img src={data.dataUrl} alt="ลายเซ็น" className="max-h-28 object-contain" />
              ) : (
                <span className="text-sm text-gray-400">ยังไม่มีลายเซ็น</span>
              )}
            </div>
          )}

          {error && <p className="rounded bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}

          <input ref={fileRef} type="file" accept="image/png,image/jpeg" onChange={onFile} className="hidden" />
        </div>

        <div className="flex justify-between gap-2 border-t border-slate-100 px-6 py-4">
          <Button
            type="button"
            variant="secondary"
            onClick={onDelete}
            disabled={!data?.hasSignature || remove.isPending || upload.isPending}
          >
            {remove.isPending ? 'กำลังลบ...' : 'ลบลายเซ็น'}
          </Button>
          <div className="flex gap-2">
            <Button type="button" variant="secondary" onClick={onClose}>ปิด</Button>
            <Button type="button" onClick={pickFile} disabled={upload.isPending}>
              {upload.isPending ? 'กำลังอัปโหลด...' : data?.hasSignature ? 'เปลี่ยนลายเซ็น' : 'อัปโหลดลายเซ็น'}
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
