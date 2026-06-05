import { useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import Button from '../../../shared/components/ui/Button'
import Card from '../../../shared/components/ui/Card'
import PageHeader from '../../../shared/components/ui/PageHeader'
import { useForm } from '../../../shared/hooks/useForm'
import { useClientDetail, useCreateClient, useUpdateClient } from '../hooks/useClients'
import type { CreateClientRequest } from '../types/client.types'

const MONTHS = [
  'มกราคม','กุมภาพันธ์','มีนาคม','เมษายน','พฤษภาคม','มิถุนายน',
  'กรกฎาคม','สิงหาคม','กันยายน','ตุลาคม','พฤศจิกายน','ธันวาคม',
]

const emptyForm: CreateClientRequest = {
  code: '',
  name: '',
  taxId: '',
  branchCode: '00000',
  address: '',
  fiscalYearStartMonth: 1,
}

export default function ClientFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = !!id && id !== 'new'
  const clientId = isEdit ? Number(id) : 0
  const navigate = useNavigate()

  const { data: existing } = useClientDetail(clientId)
  const createMutation = useCreateClient()
  const updateMutation = useUpdateClient()

  const { values, setValues, handleChange, errors, setErrors } = useForm(emptyForm)

  useEffect(() => {
    if (existing) {
      setValues({
        code: existing.code,
        name: existing.legalName,   // แก้ "ชื่อทางการ"; ชื่อ Express (existing.name) แสดงเป็นค่าอ้างอิง
        taxId: existing.taxId,
        branchCode: existing.branchCode,
        address: existing.address ?? '',
        fiscalYearStartMonth: existing.fiscalYearStartMonth,
      })
    }
  }, [existing])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setErrors({})

    try {
      if (isEdit) {
        await updateMutation.mutateAsync({
          id: clientId,
          data: {
            legalName: values.name,
            taxId: values.taxId,
            branchCode: values.branchCode,
            address: values.address || undefined,
            fiscalYearStartMonth: values.fiscalYearStartMonth,
          },
        })
      } else {
        await createMutation.mutateAsync(values)
      }
      navigate('/clients')
    } catch (err: unknown) {
      const apiErr = err as { response?: { data?: { errors?: Record<string, string[]> } } }
      if (apiErr.response?.data?.errors) {
        const flat: Record<string, string> = {}
        for (const [k, v] of Object.entries(apiErr.response.data.errors)) {
          flat[k.toLowerCase()] = v[0]
        }
        setErrors(flat)
      }
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  return (
    <div className="max-w-xl">
      <PageHeader title={isEdit ? 'แก้ไขข้อมูลลูกค้า' : 'เพิ่มลูกค้าใหม่'} />

      <Card>
        <form onSubmit={handleSubmit} className="space-y-4 p-6">
        <Field label="รหัสลูกค้า *" error={errors.code}>
          <input
            name="code"
            value={values.code}
            onChange={handleChange}
            disabled={isEdit}
            placeholder="เช่น ABC-001"
            className={inputCls(!!errors.code, isEdit)}
          />
        </Field>

        <Field label="ชื่อบริษัท (ชื่อทางการสำหรับออกงบ) *" error={errors.legalname || errors.name}>
          <input
            name="name"
            value={values.name}
            onChange={handleChange}
            placeholder="ชื่อเต็มของลูกค้าที่ใช้ในงบการเงิน"
            className={inputCls(!!(errors.legalname || errors.name))}
          />
          {isEdit && existing && existing.name !== values.name && (
            <p className="mt-1 text-xs text-gray-400">ชื่อจาก Express: {existing.name}</p>
          )}
        </Field>

        <Field label="เลขประจำตัวผู้เสียภาษี (13 หลัก) *" error={errors.taxid}>
          <input
            name="taxId"
            value={values.taxId}
            onChange={handleChange}
            placeholder="0000000000000"
            maxLength={13}
            className={inputCls(!!errors.taxid)}
          />
        </Field>

        <Field label="รหัสสาขา (5 หลัก) *" error={errors.branchcode}>
          <input
            name="branchCode"
            value={values.branchCode}
            onChange={handleChange}
            placeholder="00000"
            maxLength={5}
            className={inputCls(!!errors.branchcode)}
          />
        </Field>

        <Field label="ที่อยู่" error={errors.address}>
          <textarea
            name="address"
            value={values.address}
            onChange={handleChange}
            rows={3}
            className={inputCls(!!errors.address)}
          />
        </Field>

        <Field label="เดือนเริ่มต้นปีบัญชี *" error={errors.fiscalyearstartmonth}>
          <select
            name="fiscalYearStartMonth"
            value={values.fiscalYearStartMonth}
            onChange={handleChange}
            className={inputCls(!!errors.fiscalyearstartmonth)}
          >
            {MONTHS.map((m, i) => (
              <option key={i + 1} value={i + 1}>{m}</option>
            ))}
          </select>
        </Field>

        <div className="flex gap-3 pt-2">
          <Button
            type="submit"
            disabled={isPending}
          >
            {isPending ? 'กำลังบันทึก...' : 'บันทึก'}
          </Button>
          <Button
            type="button"
            onClick={() => navigate('/clients')}
            variant="secondary"
          >
            ยกเลิก
          </Button>
        </div>
        </form>
      </Card>
    </div>
  )
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      {children}
      {error && <p className="text-red-500 text-xs mt-1">{error}</p>}
    </div>
  )
}

function inputCls(hasError: boolean, disabled = false) {
  return [
    'w-full border rounded px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400',
    hasError ? 'border-red-400' : 'border-gray-300',
    disabled ? 'bg-gray-50 text-gray-500 cursor-not-allowed' : '',
  ].join(' ')
}
