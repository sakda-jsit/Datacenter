import { useEffect, useState } from 'react'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useOfficeProfile, useSaveOfficeProfile } from '../hooks/useOfficeProfile'

export default function OfficeProfilePage() {
  const { data, isLoading, isError } = useOfficeProfile()
  const save = useSaveOfficeProfile()

  const [name, setName] = useState('')
  const [taxId, setTaxId] = useState('')
  const [branch, setBranch] = useState('')
  const [address, setAddress] = useState('')
  const [phone, setPhone] = useState('')

  useEffect(() => {
    if (!data) return
    setName(data.officeName ?? '')
    setTaxId(data.taxId ?? '')
    setBranch(data.branchCode ?? '')
    setAddress(data.address ?? '')
    setPhone(data.phone ?? '')
  }, [data])

  const taxIdDigits = taxId.replace(/\D/g, '')
  const taxIdInvalid = taxIdDigits.length > 0 && taxIdDigits.length !== 13

  async function onSave() {
    if (taxIdInvalid) return
    await save.mutateAsync({
      officeName: name.trim(),
      taxId: taxIdDigits || null,
      branchCode: branch.trim() || null,
      address: address.trim() || null,
      phone: phone.trim() || null,
    })
  }

  return (
    <div className="max-w-2xl">
      <PageHeader title="โปรไฟล์สำนักงานบัญชี" />
      <Card className="p-6">
        <p className="mb-5 text-sm text-gray-500">
          ข้อมูลสำนักงานบัญชีของคุณ (ค่ากลาง ตั้งครั้งเดียวใช้ทุกบริษัท) — ใช้เติม
          <span className="font-medium text-slate-700"> "สำนักงานทำบัญชี" </span>
          ในแบบ ภ.ง.ด.50 ให้ทุกบริษัทอัตโนมัติ
        </p>

        {isLoading ? (
          <StateMessage>กำลังโหลด...</StateMessage>
        ) : isError ? (
          <StateMessage tone="error">โหลดข้อมูลไม่สำเร็จ</StateMessage>
        ) : (
          <div className="space-y-4">
            <Field label="ชื่อสำนักงานบัญชี (นิติบุคคล)">
              <input type="text" value={name} onChange={(e) => setName(e.target.value)}
                placeholder="เช่น สำนักงานบัญชี ... จำกัด" className={cls(false)} />
            </Field>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="เลขประจำตัวผู้เสียภาษีอากร (13 หลัก)">
                <input type="text" value={taxId} maxLength={17} onChange={(e) => setTaxId(e.target.value)}
                  placeholder="0000000000000" className={cls(taxIdInvalid)} />
                {taxIdInvalid && <p className="mt-1 text-xs text-red-500">ต้องมี 13 หลัก (ตอนนี้ {taxIdDigits.length})</p>}
              </Field>
              <Field label="รหัสสาขา">
                <input type="text" value={branch} maxLength={5} onChange={(e) => setBranch(e.target.value)}
                  placeholder="00000" className={cls(false)} />
              </Field>
            </div>
            <Field label="ที่อยู่">
              <textarea value={address} onChange={(e) => setAddress(e.target.value)} rows={2} className={cls(false)} />
            </Field>
            <Field label="โทรศัพท์">
              <input type="text" value={phone} onChange={(e) => setPhone(e.target.value)} className={cls(false)} />
            </Field>

            <div className="flex items-center gap-3 pt-1">
              <Button type="button" onClick={onSave} disabled={save.isPending || taxIdInvalid}>
                {save.isPending ? 'กำลังบันทึก...' : 'บันทึก'}
              </Button>
              {save.isSuccess && !save.isPending && <span className="text-sm text-green-600">บันทึกแล้ว ✓</span>}
              {save.isError && <span className="text-sm text-red-600">บันทึกไม่สำเร็จ — ตรวจข้อมูล</span>}
            </div>
          </div>
        )}
      </Card>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-gray-700">{label}</label>
      {children}
    </div>
  )
}

function cls(hasError: boolean) {
  return [
    'w-full rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400',
    hasError ? 'border-red-400' : 'border-gray-300',
  ].join(' ')
}
