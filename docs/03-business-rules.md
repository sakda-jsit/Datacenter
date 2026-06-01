# Business Rules

## Multi Company
- Each client company must be isolated.
- Users can only access authorized companies.
- Every transaction must belong to one company and one accounting period.

## Import Rules
- Every import batch must be recorded.
- Duplicate imports are not allowed.
- Cancelled documents must remain in history.
- Imported source document number must be traceable.
- Import errors must be reviewed before posting to production tables.

## VAT Rules
- Input VAT requires a valid tax invoice.
- Output VAT must be calculated from sales transactions.
- VAT report must reconcile with GL.
- VAT transactions must support document date and tax invoice date.

## AR Rules
- Invoice increases AR.
- Receipt decreases AR.
- Credit note decreases AR.
- Outstanding balance cannot be negative unless explicitly allowed by adjustment.

## AP Rules
- Supplier invoice increases AP.
- Payment decreases AP.
- Debit note decreases AP.
- Outstanding balance cannot be negative unless explicitly allowed by adjustment.

## Payroll Rules
- Payroll includes salary, allowance, overtime, bonus, social security, withholding tax, and other deductions.
- Payroll must generate accounting journal entries.
- Payroll reports must support PND1 and social security filing.

## Reconciliation Rules
- Bank statement must reconcile with GL.
- VAT report must reconcile with GL.
- AR/AP subledger must reconcile with GL.
- Unmatched items must remain visible until resolved.

## Closing Rules
- Closed periods cannot be modified.
- Reopen requires authorized user approval.
- Closing validation must check VAT, AR/AP, bank reconciliation, and GL balance.

## Audit Rules
- Store user, datetime, action, entity, before value, and after value.
- Import, edit, delete, approve, close, and reopen actions must be audited.
