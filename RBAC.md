# RBAC Implementation Tracker

Modern Project-Centric ERP — Role-Based Access Control
Status: **In Progress** · Last updated: 2026-08-30

## Goal

When creating or editing a **user role**, the admin can see every **page** in the
system and toggle what the role can do on that page: **View, Create, Edit, Delete**.
The role→permission grants are stored, returned on login, and (later phase) enforced
both in the UI (menu/button visibility) and on the API (endpoint authorization).

## Design decisions (locked)

- **Action taxonomy** = `View`, `Create`, `Edit`, `Delete`.
  - User's wording "add, edit, update, delete" maps as: `add` → Create, `edit`+`update` → Edit,
    `delete` → Delete. `View` is implicit (you must be able to view a page to use it).
  - To add a distinct `Update` action later, extend the `Action` enum + seed list only.
- **Permission identity** = `Code = "{module}.{page}.{action}"` (lowercase), e.g.
  `gl.journal-batches.view`, `om.quotes.create`, `ar.customers.delete`.
- **Permission entity** carries both the legacy `Module`+`Action` and the new `Page`+`Code`
  so old `Module.Action` grants still work and new page-scoped grants are first-class.
- **Admin role** is seeded with ALL permissions (wildcard behavior preserved).
- **Super admin** (company-less role) keeps unbounded access.

## Data model (Platform module)

```
Permission(Module, Page, Action, Code, Description)   -- page-scoped; Code is unique
Role(Name, Description, IsActive)
RolePermission(RoleId, PermissionId)                   -- join
UserRole(UserId, RoleId, CompanyId?)                    -- scoping
```

- `Permission.Code` is the stable key the UI and enforcement rely on.
- `Permission.Page` is the route-ish key derived from `navigation.tsx` (e.g. `journal-batches`).
- `Permission.Module` is the module prefix (e.g. `gl`, `om`, `ar`).

## Backend endpoints (Platform)

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/v1/platform/permissions/catalog` | Full page×action catalog (modules → pages → actions) for building the editor UI |
| GET | `/api/v1/platform/roles` | List roles |
| GET | `/api/v1/platform/roles/{id}` | Role (incl. permissions) |
| POST | `/api/v1/platform/roles` | Create role (super admin only) |
| PUT | `/api/v1/platform/roles/{id}` | Update role |
| GET | `/api/v1/platform/roles/{id}/matrix` | Catalog + which actions the role currently has (the editor payload) |
| PUT | `/api/v1/platform/roles/{id}/permissions` | Bulk set role permissions (array of permission ids) |
| DELETE | `/api/v1/platform/roles/{id}` | Soft delete role |

Note: the pre-existing single-permission `POST .../permissions` and
`DELETE .../permissions/{permissionId}` remain for backward compatibility.

## Frontend

- `RolesPage` (`/platform/roles`): list roles + create/edit modal that includes a
  **permission matrix** — pages grouped by module, each page row with 4 checkboxes
  (View / Create / Edit / Delete). Save calls `PUT .../permissions`.
- `navigation.tsx` is the source of the page list; the backend `PermissionCatalog`
  mirrors it and must be kept in sync (tracked item).

## Phased plan

### Phase 1 — Page-scoped permission model + role editor (IN PROGRESS)
- [x] `Permission` entity gains `Page` + `Code`
- [x] `PermissionCatalog` static registry (module → pages) in Platform
- [x] `DevSeed` seeds page×action permissions; admin gets all
- [x] `RoleController`: `GET /catalog`, `GET /roles/{id}/matrix`, `PUT /roles/{id}/permissions`
- [x] `PermissionsController`: `GET /platform/permissions` (id+code for resolver)
- [x] `RoleDto` includes permissions
- [x] Frontend `platform` api client: `getPermissionCatalog`, `getAllPermissions`, `getRoleMatrix`, `setRolePermissions`
- [x] `RolesPage` renders the page×action matrix on create/edit
- [x] `dotnet build` + `tsc --noEmit` green; live API matrix endpoint returns 200

### Phase 2 — UI enforcement (menu + action buttons)
- [ ] Gate left-nav items by `hasPermission("{module}.{page}.view")`
- [ ] Hide/disable Create/Edit/Delete buttons per permission on each page
- [ ] Wire `currentCompanyId()`-scoped visibility (already in store)

### Phase 3 — API enforcement
- [ ] `RequirePermission("{module}.{page}.{action}")` policy/attribute
- [ ] Map existing `[Authorize(Roles=...)]` endpoints to permission checks (or keep role gate as coarse, permission as fine)
- [ ] Segregation-of-duties rules (e.g. create ≠ approve same voucher)

### Phase 4 — Polish
- [ ] Clone-role action
- [ ] Bulk "grant module" / "grant all" helpers in the editor
- [ ] Filter the matrix by module (UI)
- [ ] Permission diff / audit on role change (already logged via audit service)

## Open questions / notes
- The backend `PermissionCatalog` currently duplicates `navigation.tsx`. A future
  refactor could expose nav as JSON from the API so there is one source of truth.
- Field-level restrictions (e.g. payroll compensation) are out of scope for Phase 1;
  the model supports adding them as additional permission `Page`/action values later.
