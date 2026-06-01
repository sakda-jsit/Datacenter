# Domain Model

## ClientCompany
- Id
- Code
- Name
- TaxId
- BranchCode
- Address
- FiscalYearStartMonth
- IsActive

## User
- Id
- Username
- DisplayName
- Role
- IsActive

## CompanyUserAccess
- UserId
- ClientCompanyId
- RoleInCompany

## ImportBatch
- Id
- ClientCompanyId
- SourceSystem
- ImportType
- ImportDate
- Status
- TotalRows
- SuccessRows
- ErrorRows

## Account
- Id
- ClientCompanyId
- AccountCode
- AccountName
- AccountType

## JournalEntry
- Id
- ClientCompanyId
- DocumentNo
- JournalDate
- Description
- SourceModule

## JournalEntryLine
- Id
- JournalEntryId
- AccountId
- DebitAmount
- CreditAmount

## Customer
- Id
- ClientCompanyId
- CustomerCode
- CustomerName
- TaxId

## Supplier
- Id
- ClientCompanyId
- SupplierCode
- SupplierName
- TaxId

## SalesInvoice
- Id
- ClientCompanyId
- DocumentNo
- InvoiceDate
- CustomerId
- AmountBeforeVat
- VatAmount
- TotalAmount
- OutstandingAmount

## PurchaseInvoice
- Id
- ClientCompanyId
- DocumentNo
- InvoiceDate
- SupplierId
- AmountBeforeVat
- VatAmount
- TotalAmount
- OutstandingAmount

## PayrollRun
- Id
- ClientCompanyId
- PayrollMonth
- Status
- GrossAmount
- DeductionAmount
- NetAmount

## TaxReport
- Id
- ClientCompanyId
- ReportType
- Period
- Status
- SubmittedDate

## ComplianceTask
- Id
- ClientCompanyId
- TaskType
- Period
- DueDate
- Status
- AssignedToUserId

## ClosingPeriod
- Id
- ClientCompanyId
- Period
- Status
- ClosedByUserId
- ClosedDate

## AuditLog
- Id
- ClientCompanyId
- UserId
- Action
- EntityName
- EntityId
- BeforeValue
- AfterValue
- CreatedAt
