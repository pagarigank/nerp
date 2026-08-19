# ERP Project - Agent Instructions

## Session Start Rule (MANDATORY)

At the start of EVERY session in this project (`D:\nerp`), you MUST read these files in order:

1. `D:\nerp\architecture.md` - Technical architecture, technology stack, module boundaries, data architecture
2. `D:\nerp\spec.md` - Functional requirements, module specifications, business flows, non-functional requirements
3. `D:\nerp\frontend.md` - Frontend implementation plan, UI/UX patterns, component library, screen inventory
4. `D:\nerp\todo.md` - Backend implementation TODO, phase-by-phase build plan

These files define the complete context for this ERP project. Do NOT proceed with any task until you have read and understood all four files.

## Project Context

- **Project:** Modern Project-Centric ERP (modeled on Microsoft Dynamics SL / Solomon)
- **Stack:** .NET 8, C#, ASP.NET Core, Entity Framework Core, SQL Server, React 18, TypeScript, Vite
- **Database:** SQL Server (localhost,1433) - username: sa, password: P@ssw0rd, database: erp
- **Architecture:** Modular monolith with 14 bounded-context modules
- **Current Phase:** Phase 0 complete, ready for Phase 1 (Platform/System Services)

## Module List (from spec.md)

1. Platform/System (Phase 1) - Company, Period, Segments, Users, Roles, Audit
2. General Ledger (Phase 2) - Journal batches, posting, allocations, consolidation
3. Accounts Payable (Phase 3) - Vouchers, payments, 3-way match, 1099
4. Accounts Receivable (Phase 4) - Invoices, cash receipts, statements
5. Cash Management (Phase 5) - Bank reconciliation, deposits
6. Purchasing (Phase 6) - Requisitions, POs, receipts
7. Inventory (Phase 7) - Items, warehouses, transactions, costing
8. Order Management (Phase 8) - Sales orders, shipments, invoicing
9. Bill of Materials (Phase 9) - Kitting, assembly, disassembly
10. Project Accounting (Phase 10) - Projects, budgets, billing, WIP (HIGHEST COMPLEXITY)
11. Payroll (Phase 11) - Timesheets, payroll runs, certified payroll
12. Field Service (Phase 12) - Work orders, dispatch, service contracts
13. BI/Reporting (Phase 13) - Reports, dashboards, financial statement designer
14. Integration/EDI (Phase 14) - Import/export, webhooks, EDI gateway

## Key Conventions

- **Money fields:** Always use `decimal`, never `float` or `double`
- **Timestamps:** Always UTC
- **Primary keys:** GUIDs (globally unique for cross-company reporting)
- **Soft deletes:** For entities with transaction history (never hard-delete)
- **Posted records:** Immutable - corrections via reversing entries only
- **Error responses:** Use the shared `ApiResponse<T>` envelope from `ERP.Shared.Kernel.Api`
- **API versioning:** `/api/v1/{module}/{resource}`
- **15 database schemas:** platform, gl, ap, ar, cash, pur, inv, om, bom, proj, pay, fs, rpt, int, audit

## Build & Run Commands

```bash
# Build entire solution
"C:\Program Files\dotnet\dotnet.exe" build "D:\nerp\ERP.sln"

# Run API
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5000" "C:\Program Files\dotnet\dotnet.exe" run --project "D:\nerp\src\ERP.Api\ERP.Api.csproj"

# Run tests
"C:\Program Files\dotnet\dotnet.exe" test "D:\nerp\ERP.sln"

# Frontend
cd D:\nerp\frontend && npm run dev
```

## SQL Server Access

```bash
"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE" -S localhost -U sa -P P@ssw0rd -C -d erp -Q "SELECT 1"
```

## Memory System

This project uses the `ck` memory system:
- `/ck:resume` - Get full project briefing
- `/ck:info` - Quick snapshot
- `/ck:save` - Save current session progress

## StyleCop Configuration

File headers are required on all .cs files:
```csharp
// <copyright file="FileName.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>
```

## Cross-Cutting Definition of Done

For every transactional endpoint:
- [ ] Input validation + meaningful error responses
- [ ] Authorization check (role + field-level where applicable)
- [ ] Audit log entry written automatically
- [ ] Emits/consumes correct domain events per architecture.md
- [ ] Unit tests for business rules
- [ ] Integration test for end-to-end flow
- [ ] OpenAPI documentation updated
