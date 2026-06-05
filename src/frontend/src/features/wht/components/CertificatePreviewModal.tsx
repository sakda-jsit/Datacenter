import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { whtApi } from '../services/whtApi'

interface Props {
  companyId: number
  entryIds: number[]
  onClose: () => void
}

export default function CertificatePreviewModal({ companyId, entryIds, onClose }: Props) {
  const [url, setUrl] = useState<string | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let revoked = false
    let objectUrl: string | null = null
    whtApi
      .certificate(companyId, entryIds)
      .then((blob) => {
        if (revoked) return
        objectUrl = URL.createObjectURL(blob)
        setUrl(objectUrl)
      })
      .catch(() => setError('สร้างเอกสารไม่สำเร็จ'))
    return () => {
      revoked = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [companyId, entryIds])

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-6 flex h-[90vh] w-full max-w-4xl flex-col rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-3">
          <div>
            <h2 className="text-lg font-bold text-slate-800">หนังสือรับรองหัก ณ ที่จ่าย (50 ทวิ)</h2>
            <p className="text-xs text-gray-500">{entryIds.length} ฉบับ</p>
          </div>
          <div className="flex items-center gap-2">
            {url && (
              <a href={url} download="wht-certificate.pdf">
                <Button type="button" variant="secondary">ดาวน์โหลด</Button>
              </a>
            )}
            <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
          </div>
        </div>
        <div className="flex-1 overflow-hidden p-2">
          {error && <StateMessage tone="error">{error}</StateMessage>}
          {!error && !url && <StateMessage>กำลังสร้างเอกสาร...</StateMessage>}
          {url && <iframe title="wht-certificate" src={url} className="h-full w-full rounded border border-gray-200" />}
        </div>
      </div>
    </div>
  )
}
