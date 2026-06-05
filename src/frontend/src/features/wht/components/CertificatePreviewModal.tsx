import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { whtApi } from '../services/whtApi'

interface Props {
  companyId: number
  entryIds: number[]
  onClose: () => void
}

// preview ด้วยรูป PNG (เรนเดอร์ฝั่ง server) แทน iframe PDF — เลี่ยงปัญหา PDF ขึ้นจอดำในบางเบราว์เซอร์
export default function CertificatePreviewModal({ companyId, entryIds, onClose }: Props) {
  const [images, setImages] = useState<string[] | null>(null)
  const [error, setError] = useState('')
  const [downloading, setDownloading] = useState(false)

  useEffect(() => {
    let cancelled = false
    setImages(null)
    setError('')
    whtApi
      .certificateImages(companyId, entryIds)
      .then((imgs) => { if (!cancelled) setImages(imgs) })
      .catch(() => { if (!cancelled) setError('สร้างเอกสารไม่สำเร็จ') })
    return () => { cancelled = true }
  }, [companyId, entryIds])

  async function download() {
    setDownloading(true)
    try {
      const blob = await whtApi.certificate(companyId, entryIds)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = 'wht-certificate.pdf'
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      setTimeout(() => URL.revokeObjectURL(url), 1000)
    } finally {
      setDownloading(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/50 p-4">
      <div className="my-6 flex h-[90vh] w-full max-w-4xl flex-col rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-3">
          <div>
            <h2 className="text-lg font-bold text-slate-800">หนังสือรับรองหัก ณ ที่จ่าย (50 ทวิ)</h2>
            <p className="text-xs text-gray-500">{entryIds.length} ฉบับ</p>
          </div>
          <div className="flex items-center gap-2">
            <Button type="button" variant="secondary" onClick={download} disabled={downloading}>
              {downloading ? 'กำลังเตรียม...' : 'ดาวน์โหลด PDF'}
            </Button>
            <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
          </div>
        </div>
        <div className="flex-1 overflow-y-auto bg-slate-100 p-4">
          {error && <StateMessage tone="error">{error}</StateMessage>}
          {!error && !images && <StateMessage>กำลังสร้างเอกสาร...</StateMessage>}
          {images && images.length === 0 && <StateMessage>ไม่มีเอกสาร</StateMessage>}
          {images && images.map((src, i) => (
            <img
              key={i}
              src={src}
              alt={`หนังสือรับรอง หน้า ${i + 1}`}
              className="mx-auto mb-4 w-full max-w-3xl border border-gray-300 bg-white shadow-sm"
            />
          ))}
        </div>
      </div>
    </div>
  )
}
