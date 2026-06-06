import { useState } from 'react'
import PageHeader from '../../../shared/components/ui/PageHeader'
import Tabs from '../../../shared/components/ui/Tabs'
import Card from '../../../shared/components/ui/Card'
import StateMessage from '../../../shared/components/ui/StateMessage'
import { useCurrentCompany } from '../../../shared/hooks/useCurrentCompany'
import EmployeesTab from './tabs/EmployeesTab'

type Tab = 'employees'
const TABS: { key: Tab; label: string }[] = [{ key: 'employees', label: 'ทะเบียนพนักงาน' }]

export default function PayrollPage() {
  const { companyId } = useCurrentCompany()
  const [tab, setTab] = useState<Tab>('employees')

  return (
    <div>
      <PageHeader
        title="เงินเดือน / ประกันสังคม"
        description="ทะเบียนพนักงาน + คลังหลักฐาน (รูปบัตร/หลักฐานแจ้ง ปกส.) + แจ้งเข้า-ออกประกันสังคม — ข้อมูลส่วนบุคคล (PDPA)"
      />
      <Tabs items={TABS} activeKey={tab} onChange={setTab} />
      {!companyId ? (
        <Card><StateMessage centered>เลือกบริษัทที่ header ก่อน</StateMessage></Card>
      ) : (
        tab === 'employees' && <EmployeesTab companyId={companyId} />
      )}
    </div>
  )
}
