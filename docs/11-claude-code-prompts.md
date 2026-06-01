# Claude Code Prompts

## Prompt 1 - Analyze Project Context
Read AGENTS.md, all docs, and reference files.
Summarize the target system, legacy assets, migration direction, and proposed solution structure.
Do not write code yet.

## Prompt 2 - Design Clean Architecture
Design the backend solution structure using ASP.NET Core, EF Core, SQL Server, and Clean Architecture.
Include projects, folders, major entities, services, repositories, and API modules.
Do not write implementation yet.

## Prompt 3 - Design Frontend
Design the React TypeScript frontend structure.
Include routes, feature modules, shared components, API service layer, and layout.
Use Component-Based Architecture.
Design a shared component library under `src/frontend/src/shared/components` so all pages can reuse common UI.
Include reusable components for tables, forms, filters, buttons, badges, cards, tabs, modals, pagination, loading states, empty states, and layout primitives.
Keep route pages thin; put reusable UI in shared components and feature-specific composed UI in `src/frontend/src/features/<feature>/components`.
Do not write implementation yet.

## Prompt 4 - Generate Skeleton
Generate the solution skeleton only.
Do not implement business logic yet.

## Prompt 5 - Implement First Module
Implement Client Management end-to-end:
- Entity
- DTO
- Service
- Controller
- EF configuration
- Migration
- React pages
- Shared reusable frontend components where common UI is needed
- Feature components that compose shared components
- API service

## Prompt 6 - Convert Reference Logic
Analyze `/reference/express` and `/reference/financial`.
Create C# service design for importing trial balance and generating financial statements.
Do not copy Python code directly.
