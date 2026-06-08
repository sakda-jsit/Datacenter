import { useState } from 'react'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useResetTemplate, useTaskTemplates, useUpsertTemplate } from '../hooks/useCompliance'
import type { ComplianceTaskTemplateDto } from '../types/compliance.types'

interface Props {
  companyId: number
  companyName?: string
}

const SOURCE_BADGE: Record<string, { label: string; cls: string }> = {
  default: { label: 'ค่าเริ่มต้น', cls: 'bg-slate-100 text-slate-500' },
  global: { label: 'จากมาตรฐาน', cls: 'bg-sky-100 text-sky-700' },
  company: { label: 'ตั้งเฉพาะบริษัท', cls: 'bg-violet-100 text-violet-700' },
}

function dueLabel(t: ComplianceTaskTemplateDto): string {
  const d = t.dueDay ?? t.defaultDueDay
  return d <= 0 ? 'สิ้นเดือนถัดไป' : `วันที่ ${d} ของเดือนถัดไป`
}

export default function TaskTemplateSettings({ companyId, companyName }: Props) {
  // ระดับ: global (ทุกบริษัท) หรือ company (เฉพาะบริษัทที่เลือก)
  const [scope, setScope] = useState<'global' | 'company'>('global')
  const effectiveScope = scope === 'company' && companyId > 0 ? 'company' : 'global'
  const scopeCompanyId = effectiveScope === 'company' ? companyId : undefined

  const { data: templates, isLoading } = useTaskTemplates(scopeCompanyId)
  const upsert = useUpsertTemplate()
  const reset = useResetTemplate()

  function save(t: ComplianceTaskTemplateDto, patch: Partial<Pick<ComplianceTaskTemplateDto, 'enabled' | 'dueDay'>>) {
    upsert.mutate({
      clientCompanyId: effectiveScope === 'company' ? companyId : null,
      taskType: t.taskType,
      enabled: patch.enabled ?? t.enabled,
      dueDay: patch.dueDay !== undefined ? patch.dueDay : t.dueDay,
    })
  }

  return (
    <Card className="overflow-hidden">
      <div className="border-b border-slate-100 px-5 py-4">
        <p className="text-sm font-extrabold text-slate-800">ตั้งค่างานประจำ (template 2 ระดับ)</p>
        <p className="mt-0.5 text-xs text-slate-500">กำหนดว่างานประเภทไหนใช้กับบริษัทไหน + วันครบกำหนด — ใช้ตอนสร้างงานรายเดือน</p>
        <div className="mt-3 inline-flex rounded-lg border border-slate-200 p-0.5 text-sm">
          <button type="button" onClick={() => setScope('global')}
            className={`rounded-md px-3 py-1.5 font-medium ${effectiveScope === 'global' ? 'bg-sky-600 text-white' : 'text-slate-600'}`}>
            ทุกบริษัท (มาตรฐาน)
          </button>
          <button type="button" onClick={() => setScope('company')} disabled={companyId === 0}
            className={`rounded-md px-3 py-1.5 font-medium disabled:opacity-40 ${effectiveScope === 'company' ? 'bg-violet-600 text-white' : 'text-slate-600'}`}
            title={companyId === 0 ? 'เลือกบริษัทที่แถบด้านบนก่อน' : ''}>
            เฉพาะบริษัท{companyId > 0 && companyName ? `: ${companyName}` : ''}
          </button>
        </div>
        {effectiveScope === 'company' && (
          <p className="mt-2 text-xs text-violet-600">
            ค่าที่ไม่ได้ตั้งเฉพาะบริษัทจะใช้ตาม “มาตรฐาน” อัตโนมัติ — ตั้งค่าที่นี่เพื่อทับเฉพาะบริษัทนี้
          </p>
        )}
      </div>

      {isLoading || !templates ? (
        <StateMessage>กำลังโหลด...</StateMessage>
      ) : (
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-gray-600">
            <tr>
              <th className="px-4 py-2 text-left">ประเภทงาน</th>
              <th className="px-4 py-2 text-center">ใช้งาน</th>
              <th className="px-4 py-2 text-left">วันครบกำหนด (ของเดือนถัดไป)</th>
              <th className="px-4 py-2 text-left">ที่มา</th>
              <th className="px-4 py-2"></th>
            </tr>
          </thead>
          <tbody>
            {templates.map((t) => {
              const badge = SOURCE_BADGE[t.source]
              return (
                <tr key={t.taskType} className={`border-t border-gray-100 ${!t.enabled ? 'bg-slate-50/60' : ''}`}>
                  <td className="px-4 py-2 font-medium text-slate-800">{t.taskTypeName}</td>
                  <td className="px-4 py-2 text-center">
                    <label className="inline-flex cursor-pointer items-center">
                      <input type="checkbox" checked={t.enabled} disabled={upsert.isPending}
                        onChange={(e) => save(t, { enabled: e.target.checked })} className="h-4 w-4" />
                    </label>
                  </td>
                  <td className="px-4 py-2">
                    <div className="flex items-center gap-2">
                      <input type="number" min={0} max={31}
                        value={t.dueDay ?? ''} placeholder={String(t.defaultDueDay || 0)}
                        disabled={!t.enabled || upsert.isPending}
                        onChange={(e) => save(t, { dueDay: e.target.value === '' ? null : Number(e.target.value) })}
                        className="w-20 rounded border border-gray-300 px-2 py-1 text-sm disabled:bg-slate-100" />
                      <span className="text-xs text-gray-400">{dueLabel(t)} {t.defaultDueDay === 0 && '(0 = สิ้นเดือน)'}</span>
                    </div>
                  </td>
                  <td className="px-4 py-2">
                    <span className={`dc-pill ${badge.cls}`}>{badge.label}</span>
                  </td>
                  <td className="px-4 py-2 text-right">
                    {effectiveScope === 'company' && t.source === 'company' && (
                      <button type="button" disabled={reset.isPending}
                        onClick={() => reset.mutate({ clientCompanyId: companyId, taskType: t.taskType })}
                        className="text-xs text-slate-500 hover:text-slate-700 hover:underline">คืนค่ามาตรฐาน</button>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      )}
    </Card>
  )
}
