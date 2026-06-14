import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import Button from '../../../shared/components/ui/Button'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { vatApi } from '../services/vatApi'

interface Props {
  companyId: number
  onClose: () => void
}

/**
 * แก้ไขการแมพ DEPCOD → เลขสาขา RD ต่อบริษัท + ปุ่มดึง DEPCOD ใหม่จาก Express (resync).
 * ถ้าไม่ได้แมพ จะใช้กฎอัตโนมัติ (HO/ว่าง → 00000, BR01 → 00001).
 */
export default function Pp30BranchMappingModal({ companyId, onClose }: Props) {
  const qc = useQueryClient()
  const { data, isLoading, isError } = useQuery({
    queryKey: ['vat-branch-mappings', companyId],
    queryFn: () => vatApi.branchMappings(companyId),
    enabled: companyId > 0,
  })

  // edit state: departmentCode -> { rdBranchNo, isHeadOffice }
  const [edits, setEdits] = useState<Record<string, { rdBranchNo: string; isHeadOffice: boolean }>>({})
  useEffect(() => {
    if (!data) return
    setEdits(Object.fromEntries(data.map((m) => [m.departmentCode, { rdBranchNo: m.rdBranchNo, isHeadOffice: m.isHeadOffice }])))
  }, [data])

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['vat-branch-mappings', companyId] })
    qc.invalidateQueries({ queryKey: ['vat-pp30-branches', companyId] })
  }

  const save = useMutation({
    mutationFn: (v: { departmentCode: string; rdBranchNo: string; isHeadOffice: boolean }) =>
      vatApi.upsertBranchMapping(companyId, v),
    onSuccess: invalidate,
  })
  const reset = useMutation({
    mutationFn: (departmentCode: string) => vatApi.deleteBranchMapping(companyId, departmentCode),
    onSuccess: invalidate,
  })
  const resync = useMutation({
    mutationFn: () => vatApi.resyncDepartments(),
    onSuccess: invalidate,
  })

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm">
      <div className="my-8 w-full max-w-2xl rounded-2xl bg-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <h2 className="text-lg font-bold text-slate-800">แมพเลขสาขา (DEPCOD → เลขสาขา ภ.พ.30)</h2>
          <button type="button" onClick={onClose} className="text-2xl leading-none text-slate-400 hover:text-slate-600">×</button>
        </div>

        <div className="px-6 py-4">
          <div className="mb-3 flex items-center justify-between">
            <p className="text-xs text-gray-500">
              ใส่เลขสาขาตามที่จดทะเบียนกับสรรพากร (สำนักงานใหญ่ = 00000) · ถ้าไม่แมพจะใช้กฎอัตโนมัติ
            </p>
            <Button type="button" variant="secondary" onClick={() => resync.mutate()} disabled={resync.isPending}>
              {resync.isPending ? 'กำลังดึง...' : '⟳ ดึง DEPCOD ใหม่จาก Express'}
            </Button>
          </div>

          {resync.isSuccess && (
            <p className="mb-2 rounded bg-green-50 px-3 py-1.5 text-xs text-green-700">
              ดึงใหม่แล้ว: {resync.data.companiesUpdated}/{resync.data.companiesProcessed} บริษัท · {resync.data.totalEntries} รายการ · มีหลายสาขา {resync.data.companiesWithBranches}
            </p>
          )}

          {isError && <StateMessage tone="error">โหลดไม่สำเร็จ</StateMessage>}
          {isLoading && <StateMessage>กำลังโหลด...</StateMessage>}

          {data && data.length === 0 && (
            <StateMessage centered>ยังไม่มีข้อมูล DEPCOD — กด "ดึง DEPCOD ใหม่จาก Express"</StateMessage>
          )}

          {data && data.length > 0 && (
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-xs text-gray-600">
                <tr>
                  <th className="px-3 py-2 text-left font-medium">DEPCOD</th>
                  <th className="px-3 py-2 text-right font-medium">รายการ</th>
                  <th className="px-3 py-2 text-left font-medium">เลขสาขา RD</th>
                  <th className="px-3 py-2 text-center font-medium">สนญ.</th>
                  <th className="px-3 py-2 text-left font-medium"></th>
                </tr>
              </thead>
              <tbody>
                {data.map((m) => {
                  const e = edits[m.departmentCode] ?? { rdBranchNo: m.rdBranchNo, isHeadOffice: m.isHeadOffice }
                  const setE = (patch: Partial<typeof e>) =>
                    setEdits((p) => ({ ...p, [m.departmentCode]: { ...e, ...patch } }))
                  return (
                    <tr key={m.departmentCode || '(blank)'} className="border-t border-gray-100">
                      <td className="px-3 py-2 font-mono">
                        {m.displayCode}
                        {!m.isMapped && <span className="ml-2 text-xs text-amber-500">(อัตโนมัติ)</span>}
                      </td>
                      <td className="px-3 py-2 text-right text-gray-500">{m.entryCount.toLocaleString()}</td>
                      <td className="px-3 py-2">
                        <input
                          value={e.rdBranchNo}
                          onChange={(ev) => setE({ rdBranchNo: ev.target.value.replace(/\D/g, '') })}
                          className="w-28 rounded border border-gray-300 px-2 py-1 font-mono text-sm focus:outline-none focus:ring-1 focus:ring-blue-400"
                        />
                      </td>
                      <td className="px-3 py-2 text-center">
                        <input type="checkbox" checked={e.isHeadOffice} onChange={(ev) => setE({ isHeadOffice: ev.target.checked })} />
                      </td>
                      <td className="px-3 py-2">
                        <div className="flex gap-1">
                          <Button type="button" onClick={() => save.mutate({ departmentCode: m.departmentCode, ...e })} className="px-2 py-1 text-xs">
                            บันทึก
                          </Button>
                          {m.isMapped && (
                            <Button type="button" variant="secondary" onClick={() => reset.mutate(m.departmentCode)} className="px-2 py-1 text-xs">
                              คืนค่า
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}
        </div>

        <div className="flex justify-end border-t border-slate-100 px-6 py-3">
          <Button type="button" variant="secondary" onClick={onClose}>ปิด</Button>
        </div>
      </div>
    </div>
  )
}
