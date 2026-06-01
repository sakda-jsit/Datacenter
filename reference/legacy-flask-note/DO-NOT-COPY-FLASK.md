# Do Not Copy Legacy Flask Runtime

The following old prototype parts were intentionally excluded:

- Flask route files
- Jinja templates
- static CSS
- cache files
- user JSON
- secrets
- pycache

Reason:
The target architecture is React + ASP.NET Core + SQL Server + Clean Architecture.

Frontend note:
React screens must not copy the Jinja page structure. Build the frontend with Component-Based Architecture and a shared component library under `src/frontend/src/shared/components`, then compose feature pages from reusable components.
