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

---

# Entities เพิ่มเติม (Requirement v11)

> ระดับแนวคิด — field จริงให้ยืนยันตอนออกแบบ EF; ทุก entity ผูก ClientCompanyId + FiscalYear และมี field-level audit

## AdjustmentEntry
- Id, ClientCompanyId, FiscalYear, AccountId
- DebitAmount, CreditAmount
- SourceType (Leasing / Loan / Manual / Tax / Other)
- SourceReference, Reason, AttachmentId

## LeasingLoanSchedule
- Id, ClientCompanyId, FiscalYear, ContractType (Leasing / Loan)
- ContractRef, Principal, Rate, StartDate, EndDate
- (lines) PeriodScheduleLine: Period, Principal, Interest, Balance
- → generate AdjustmentEntry เข้า TB ปีปัจจุบัน

## FsGroupCode (DBD taxonomy master)
- Code (A1/A2/L1...), Name, StatementType (BS/PL/CAP), ParentCode, SortOrder
- (อ้างอิง `npae_com-oth_...xls`)

## NoteTemplate / NoteDataBinding
- NoteTemplate: Id, ClientCompanyId, NoteNo, TitleText, BodyText (User-editable), EffectiveYear
- NoteDataBinding: NoteNo, LineRef, SourceFormula (ดึงจาก TB ปีปัจจุบัน/ปีก่อน)

## FixedAsset
- Id, ClientCompanyId, AssetCode, AssetName, AssetTypeId
- AcquisitionDate, Cost
- BookDepRate, TaxDepRate (default จาก AssetType master, override ได้)
- Status (Active / Disposed / Sold / WrittenOff), DisposalDate, DisposalProceeds
- (computed) GainLossOnDisposal = DisposalProceeds − NetBookValue

## AssetTypeMaster
- Id, Name, DefaultBookDepRate, DefaultTaxDepRate, DefaultUsefulLife

## DepreciationScheduleLine
- Id, FixedAssetId, Period, Basis (Book / Tax)
- DepThisPeriod, AccumulatedDep, NetBookValue

## PrepaidExpense
- Id, ClientCompanyId, Description, DocumentRef, Amount, StartDate, EndDate
- (lines) PrepaidAmortizationLine: Period, AmortizedAmount, AccumulatedAmortized, Remaining

## StockItem / Warehouse / StockBalance
- Warehouse: Id, ClientCompanyId, Code, Name
- StockItem: Id, ClientCompanyId, ItemCode, ItemName
- StockBalance: Id, FiscalYear, ItemId, WarehouseId, Quantity, FifoUnitCost, Amount (snapshot จาก Express)

## InventoryReconciliation
- Id, ClientCompanyId, FiscalYear, ExpressAmount, TbAmount, Variance
- Status (Matched / Unmatched / PendingReview), Note

## TaxFiling
- Id, ClientCompanyId, FormType (PP30 / PND3 / PND53 / PND50)
- TaxPeriod, FilingType (Normal / Additional), SubmissionSeq
- IncomeTotal, TaxTotal, Surcharge, Penalty, PaidTotal
- FilingDate, PaymentDate, ReferenceNo
- SourcePdfAttachmentId, ReconcileStatus (Matched / Mismatch / Pending)

## BankStatement / BankStatementLine
- BankStatement: Id, ClientCompanyId, BankAccountId, Period
- BankStatementLine: Date, Description, Amount, Reference

## ReconciliationItem
- Id, ClientCompanyId, FiscalYear, Type (AR / AP)
- ExpressDocRef, BankStatementLineId, Amount, Wht, BankCharge, NetAmount, Variance
- Status (Matched / Partial / Unmatched)

## SubsequentPaymentCheck
- Id, ClientCompanyId, FiscalYear, AccrualDocRef, Account, Vendor, AccruedAmount, AccrualDate
- NextYearPaymentRef, PaidAmount, PaymentDate, Status (Paid / Partial / Unpaid / Unmatched), SourceData

## Attachment
- Id, ClientCompanyId, Module, RecordId, DocumentType, FileName, FilePath, Checksum
- UploadedByUserId, UploadedDate, FiscalYear, VerifyStatus
- RetentionUntil (≥ 10 ปี)

## FieldAuditLog (ขยายจาก AuditLog)
- Id, ClientCompanyId, Module, RecordId, FieldName, OldValue, NewValue
- ActionType (Create / Update / Delete), UserId, ChangedAt, Reason, AttachmentId

## ImportSnapshot
- Id, ClientCompanyId, FiscalYear, SourceType (TB/GL/JV/RE/PV/VAT/STOCK)
- SnapshotDate, SourceFilePath, Checksum, RowCount, Status (เก็บถาวร ≥ 10 ปี)

## ReportPackage
- Id, ClientCompanyId, FiscalYear, PackageType (Auditor / DBD / RevenueDept / SSO / Other)
- Status (Draft / Review / Final), Version, IsLocked, GeneratedByUserId, GeneratedDate
