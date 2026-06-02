import { useEffect, useState } from 'react'
import type { AxiosError } from 'axios'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useAuth } from '../../../shared/hooks/useAuth'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import MonthCard from '../components/MonthCard'
import ClosingValidationPanel from '../components/ClosingValidationPanel'
import { useClosingPeriods, useClosingValidation, useCloseMutations } from '../hooks/useClosingPeriod'

function apiErrorMessage(err: unknown): string {
  const axiosErr = err as AxiosError<{ title?: string }>
  return axiosErr?.response?.data?.title ?? 'เกิดข้อผิดพลาด กรุณาลองใหม่'
}

export default function ClosingPeriodPage() {
  const currentYear = new Date().getFullYear()
  const { companyId } = useCurrentCompany()
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'

  const [year, setYear] = useState(currentYear)
  const [selectedMonth, setSelectedMonth] = useState<number | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const { data: overview, isLoading, isError } = useClosingPeriods(companyId, year)
  const { data: validation, isLoading: validating } = useClosingValidation(companyId, year, selectedMonth)
  const { close, reopen, lock } = useCloseMutations(companyId, year)

  // เปลี่ยนบริษัท/ปี แล้วล้างเดือนที่เลือกและ error เดิม
  useEffect(() => {
    setSelectedMonth(null)
    setActionError(null)
  }, [companyId, year])

  const busy = close.isPending || reopen.isPending || lock.isPending

  function runAction(fn: () => Promise<unknown>) {
    setActionError(null)
    fn().catch((err) => setActionError(apiErrorMessage(err)))
  }

  return (
    <div>
      <PageHeader
        title="ปิดรอบบัญชี"
        description="ตรวจสอบความพร้อม ปิดงวด ล็อกถาวร และเปิดงวดบัญชีใหม่"
        action={
          <div className="flex items-end gap-2">
            <label className="flex flex-col text-xs font-medium text-slate-600">
              ปีบัญชี (AD)
              <input
                type="number"
                value={year}
                onChange={(e) => setYear(Number(e.target.value))}
                min={2000}
                max={2100}
                className="mt-1 w-24 rounded border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400"
              />
            </label>
          </div>
        }
      />

      {!companyId && (
        <Card>
          <StateMessage centered>เลือกบริษัทที่ header ก่อน จึงจะจัดการการปิดงวดได้</StateMessage>
        </Card>
      )}

      {companyId > 0 && (
        <>
          {isError && <StateMessage tone="error">โหลดข้อมูลไม่สำเร็จ กรุณาลองใหม่</StateMessage>}
          {isLoading && (
            <Card>
              <StateMessage centered>กำลังโหลด...</StateMessage>
            </Card>
          )}

          {overview && !overview.isDefined && (
            <Card className="px-6 py-5">
              <StateMessage centered>
                {`ยังไม่ได้นำเข้านิยามรอบบัญชีของปี ${overview.year} — กรุณานำเข้าข้อมูล Express (ISPRD) ของปีนี้ก่อน`}
              </StateMessage>
            </Card>
          )}

          {overview && overview.isDefined && (
            <>
              <Card className="mb-4 px-6 py-4">
                <p className="text-lg font-semibold text-slate-800">
                  {overview.clientCode} — {overview.clientName}
                </p>
                <p className="text-sm text-gray-500">สถานะการปิดงวด ปี {overview.year} · {overview.months.length} งวด</p>
              </Card>

              <div className="grid grid-cols-2 gap-4 lg:grid-cols-[2fr_1fr]">
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                  {overview.months.map((m) => (
                    <MonthCard
                      key={m.month}
                      data={m}
                      selected={selectedMonth === m.month}
                      onSelect={(month) => {
                        setSelectedMonth(month)
                        setActionError(null)
                      }}
                    />
                  ))}
                </div>

                <div>
                  {selectedMonth == null ? (
                    <Card className="p-5">
                      <StateMessage centered>เลือกเดือนเพื่อตรวจสอบและจัดการการปิดงวด</StateMessage>
                    </Card>
                  ) : (
                    <ClosingValidationPanel
                      year={year}
                      month={selectedMonth}
                      validation={validation}
                      loading={validating}
                      isAdmin={isAdmin}
                      busy={busy}
                      errorMessage={actionError}
                      onClose={() => runAction(() => close.mutateAsync(selectedMonth))}
                      onReopen={() => runAction(() => reopen.mutateAsync({ month: selectedMonth }))}
                      onLock={() => runAction(() => lock.mutateAsync(selectedMonth))}
                    />
                  )}
                </div>
              </div>
            </>
          )}
        </>
      )}
    </div>
  )
}
