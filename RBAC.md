# RBAC Implementation Tracker

## Goal

A working role-based access control system where, when creating or editing a role, an admin
sees every page grouped by module and can toggle per-page actions (View / Create / Edit / Delete).
Permissions follow the canonical code `module.page.action` (e.g. `gl.journal-batches.view`).

## Design

- **Actions:** View, Create, Edit, Delete (the user's "add" maps to Create; "edit" + "update"
  map to Edit; Delete is separate; View is implicit for opening a page).
- **Permission `Code`** = `{module}.{page}.{action}` (lowercase). This is the stable key the UI
  and enforcement rely on.
- **Legacy wildcard forms** are also honored by the matcher so existing roles keep working:
  - `module.*.action` → grants that action on every page of the module
  - `module.page.*` → grants every action on that page
  - `*.*` / `*.*.*` → full access
- **Admin role** gets all page-scoped permissions.

## Data model

```text
Permission(Id, Module, Page, Action, Code, Description)
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
| GET | `/api/v1/platform/permissions` | All permissions (id + code) for the client-side id resolver |
| GET | `/api/v1/platform/roles` | List roles |
| GET | `/api/v1/platform/roles/{id}` | Role (incl. permissions) |
| POST | `/api/v1/platform/roles` | Create role (company admin or super admin) |
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
- `stores/authStore.ts` → `hasPermission(code)`: enhanced matcher supporting page-scoped,
  legacy (`module.action` → `module.*.action`) and wildcard codes.
- `hooks/usePagePermission.ts` → `usePagePermission(module, page)` returns
  `{canView, canCreate, canEdit, canDelete}`; pages use it to hide action buttons.
  `modulePageFromRoute(to)` derives module + page from a nav route.
- `layouts/MainLayout.tsx`: left-nav and Ctrl-K command palette are gated by view permission.
- `navigation.tsx` is the source of the page list; the backend `PermissionCatalog`
  mirrors it and must be kept in sync.

## Phased plan

### Phase 1 — Page-scoped permission model + role editor (DONE)
- [x] `Permission` entity gains `Page` + `Code`
- [x] `PermissionCatalog` static registry (module → pages) in Platform
- [x] `DevSeed` seeds page×action permissions; admin gets all
- [x] `RoleController`: `GET /catalog`, `GET /roles/{id}/matrix`, `PUT /roles/{id}/permissions`
- [x] `PermissionsController`: `GET /platform/permissions` (id+code for resolver)
- [x] `RoleDto` includes permissions
- [x] Frontend `platform` api client: `getPermissionCatalog`, `getAllPermissions`, `getRoleMatrix`, `setRolePermissions`
- [x] `RolesPage` renders the page×action matrix on create/edit
- [x] `dotnet build` + `tsc --noEmit` green; live API matrix endpoint returns 200
- [x] DB: 52 legacy perms + 688 page-scoped perms = 740; unique index on `Code`

### Phase 2 — UI enforcement (DONE)
- [x] `hasPermission(code)` enhanced to match page-scoped (`module.page.action`) + legacy
      (`module.action` → `module.*.action`) + wildcard (`*.*` / `module.*.action` / `module.page.*`)
- [x] `usePagePermission(module, page)` hook returns `{canView,canCreate,canEdit,canDelete}`
- [x] `modulePageFromRoute(to)` derives module + page from a nav `to` route
- [x] `MainLayout` left-nav gated: a module shows only sub-pages the user can `view`; empty modules hidden
- [x] Command-palette (Ctrl-K) search results also gated by view permission
- [x] **Permission vocabulary unified**: login now emits canonical `Permission.Code`
      (`module.page.action`) instead of the old `Module.Action`. Legacy rows backfilled
      to `module.*.action`. Company admin keeps full access via `*.*.*` + module wildcards.
- [x] **Bug fixed**: `SetPermissions` (bulk PUT) threw duplicate-key 500 because the role was
      fetched without `.Include(Permissions)` (current set empty → re-added existing grants).
      Added the include; now 204.
- [x] Verified: limited demo user `rbacviewer@erp.com` / `password123` (role "AP Viewer")
      sees exactly 2 of 188 nav items (`/platform/roles`, `/ap/vendors`); company admin sees all.

### Phase 3 — API enforcement (NEXT)
- [ ] `RequirePermission("{module}.{page}.{action}")` policy/attribute on controllers
- [ ] Map existing `[Authorize(Roles=...)]` endpoints to permission checks (or keep role gate as coarse, permission as fine)
- [ ] Segregation-of-duties rules (e.g. create ≠ approve same voucher)

### Phase 4 — Polish
- [ ] Clone-role action
- [ ] Bulk "grant module" / "grant all" helpers in the editor
- [ ] Filter the matrix by module (UI)
- [ ] Wire `usePagePermission` into individual page action buttons (e.g. New/Edit/Delete)
