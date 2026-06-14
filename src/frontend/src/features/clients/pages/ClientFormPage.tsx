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

// ช่องที่อยู่แยก (ใช้ตอนแก้ไข — สำหรับฟอร์มราชการ เช่น ภ.ง.ด.50)
type FormState = CreateClientRequest & {
  businessActivity: string
  addrBuilding: string; addrRoomNo: string; addrFloor: string; addrVillage: string
  addrHouseNo: string; addrMoo: string; addrSoi: string; addrRoad: string
  addrSubDistrict: string; addrDistrict: string; addrProvince: string
}

const emptyForm: FormState = {
  code: '',
  name: '',
  taxId: '',
  branchCode: '00000',
  address: '',
  fiscalYearStartMonth: 1,
  ssoAccountNo: '',
  ssoBranchCode: '000000',
  phone: '',
  postalCode: '',
  businessActivity: '',
  addrBuilding: '', addrRoomNo: '', addrFloor: '', addrVillage: '',
  addrHouseNo: '', addrMoo: '', addrSoi: '', addrRoad: '',
  addrSubDistrict: '', addrDistrict: '', addrProvince: '',
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
        ssoAccountNo: existing.ssoAccountNo ?? '',
        ssoBranchCode: existing.ssoBranchCode ?? '000000',
        phone: existing.phone ?? '',
        postalCode: existing.postalCode ?? '',
        businessActivity: existing.businessActivity ?? '',
        addrBuilding: existing.addressDetail?.building ?? '',
        addrRoomNo: existing.addressDetail?.roomNo ?? '',
        addrFloor: existing.addressDetail?.floor ?? '',
        addrVillage: existing.addressDetail?.village ?? '',
        addrHouseNo: existing.addressDetail?.houseNo ?? '',
        addrMoo: existing.addressDetail?.moo ?? '',
        addrSoi: existing.addressDetail?.soi ?? '',
        addrRoad: existing.addressDetail?.road ?? '',
        addrSubDistrict: existing.addressDetail?.subDistrict ?? '',
        addrDistrict: existing.addressDetail?.district ?? '',
        addrProvince: existing.addressDetail?.province ?? '',
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
            ssoAccountNo: values.ssoAccountNo || undefined,
            ssoBranchCode: values.ssoBranchCode || undefined,
            phone: values.phone || undefined,
            postalCode: values.postalCode || undefined,
            businessActivity: values.businessActivity || undefined,
            addressDetail: {
              building: values.addrBuilding || undefined,
              roomNo: values.addrRoomNo || undefined,
              floor: values.addrFloor || undefined,
              village: values.addrVillage || undefined,
              houseNo: values.addrHouseNo || undefined,
              moo: values.addrMoo || undefined,
              soi: values.addrSoi || undefined,
              road: values.addrRoad || undefined,
              subDistrict: values.addrSubDistrict || undefined,
              district: values.addrDistrict || undefined,
              province: values.addrProvince || undefined,
            },
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

        {isEdit && (
          <Field label="ประกอบกิจการ (ประเภทกิจการ — สำหรับ ภ.ง.ด.50)" error={errors.businessactivity}>
            <input
              name="businessActivity"
              value={values.businessActivity}
              onChange={handleChange}
              placeholder="เช่น รับเหมาก่อสร้าง, ขายส่งวัสดุก่อสร้าง"
              className={inputCls(false)}
            />
          </Field>
        )}

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

        {isEdit && (
          <div className="space-y-4 rounded-lg border border-slate-100 bg-slate-50/50 p-4">
            <p className="text-sm font-semibold text-slate-700">ประกันสังคม (นายจ้าง) — สำหรับ สปส.1-10</p>
            <div className="grid grid-cols-2 gap-3">
              <Field label="เลขที่บัญชีนายจ้าง ปกส. (10 หลัก)" error={errors.ssoaccountno}>
                <input name="ssoAccountNo" value={values.ssoAccountNo ?? ''} onChange={handleChange} placeholder="2000398553" maxLength={15} className={inputCls(false)} />
              </Field>
              <Field label="ลำดับที่สาขา ปกส. (6 หลัก)" error={errors.ssobranchcode}>
                <input name="ssoBranchCode" value={values.ssoBranchCode ?? ''} onChange={handleChange} placeholder="000000" maxLength={6} className={inputCls(false)} />
              </Field>
              <Field label="โทรศัพท์" error={errors.phone}>
                <input name="phone" value={values.phone ?? ''} onChange={handleChange} className={inputCls(false)} />
              </Field>
              <Field label="รหัสไปรษณีย์" error={errors.postalcode}>
                <input name="postalCode" value={values.postalCode ?? ''} onChange={handleChange} maxLength={10} className={inputCls(false)} />
              </Field>
            </div>
          </div>
        )}

        {isEdit && (
          <details className="rounded-lg border border-slate-100 bg-slate-50/50 p-4">
            <summary className="cursor-pointer text-sm font-semibold text-slate-700">
              ที่อยู่แยกช่อง (สำหรับฟอร์มราชการ เช่น ภ.ง.ด.50)
            </summary>
            <p className="mt-1 text-xs text-gray-400">
              เติมอัตโนมัติจากที่อยู่ตอนนำเข้า — แก้ไขให้ตรงกับทะเบียนได้ (ใช้รหัสไปรษณีย์ช่องด้านบน)
            </p>
            <div className="mt-3 grid grid-cols-2 gap-3">
              <Field label="อาคาร"><input name="addrBuilding" value={values.addrBuilding} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="ห้องเลขที่"><input name="addrRoomNo" value={values.addrRoomNo} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="ชั้นที่"><input name="addrFloor" value={values.addrFloor} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="หมู่บ้าน"><input name="addrVillage" value={values.addrVillage} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="เลขที่"><input name="addrHouseNo" value={values.addrHouseNo} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="หมู่ที่"><input name="addrMoo" value={values.addrMoo} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="ตรอก/ซอย"><input name="addrSoi" value={values.addrSoi} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="ถนน"><input name="addrRoad" value={values.addrRoad} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="ตำบล/แขวง"><input name="addrSubDistrict" value={values.addrSubDistrict} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="อำเภอ/เขต"><input name="addrDistrict" value={values.addrDistrict} onChange={handleChange} className={inputCls(false)} /></Field>
              <Field label="จังหวัด"><input name="addrProvince" value={values.addrProvince} onChange={handleChange} className={inputCls(false)} /></Field>
            </div>
          </details>
        )}

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
