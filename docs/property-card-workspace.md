# Property Card workspace implementation

The registry now opens a dedicated Property Card workspace. List and Catalogue retain shared pagination, filters, sorting, page size and the existing encoder filter. No database schema or persistence-service changes were made.

## Files modified

- iLgs/Controllers/PropertyCardController.cs — adds read-only workspace, summary, tab, choices, unit-context, document-context, accountability and history endpoints.
- iLgs/Views/PropertyCard/Index.cshtml — keeps the registry; replaces nested details with Workspace navigation.
- iLgs/Views/PropertyCard/_PropertyCardItem.cshtml — reorganizes acquisitions.
- iLgs/Scripts/property-card-index.js — routes View Property Card to Workspace (created during the preceding List/Catalogue revision).
- iLgs/ePSMS.csproj — registers new source and content files.

## Files added

- iLgs/Models/PropertyCardWorkspaceViewModels.cs
- iLgs/Services/PropertyCard/PropertyCardWorkspaceService.cs
- iLgs/Views/PropertyCard/Workspace.cshtml
- iLgs/Views/PropertyCard/_ClientConfig.cshtml
- iLgs/Views/PropertyCard/_UnitTemplates.cshtml
- iLgs/Views/PropertyCard/_ValidationTemplate.cshtml
- iLgs/Views/PropertyCard/_WorkspaceUnits.cshtml
- iLgs/Views/PropertyCard/_WorkspaceAccountability.cshtml
- iLgs/Views/PropertyCard/_WorkspaceAccountabilityScope.cshtml
- iLgs/Views/PropertyCard/_WorkspaceDocuments.cshtml
- iLgs/Views/PropertyCard/_WorkspaceDocumentScope.cshtml
- iLgs/Views/PropertyCard/_WorkspaceHistory.cshtml
- iLgs/Scripts/property-card-legacy.js
- iLgs/Scripts/property-card-view.js
- iLgs/Content/property-card-legacy.css
- iLgs/Content/property-card-view.css

The preceding registry revision also added property-card-index.js and property-card-index.css. This report is docs/property-card-workspace.md.

## Workflow and presentation

1. Open a card from either registry view.
2. Overview shows card identity, fund, description, audit information and inventory position.
3. Acquisitions uses seven columns: Reference, Acquisition, PO Owner / Location, Movement, Balance, Posting and Actions. View Details expands a structured summary. More contains Edit, the two PO reports, Post/Unpost and permitted Transfer/Delete actions.
4. Individual Units selects an acquisition/location and loads its existing Vehicle, Other, Land or Building grid. Property numbers remain distinct from Property Card numbers. Existing category editors remain in use.
5. Accountability reuses the existing ItemCard PAR/ICS query and grid for a selected unit.
6. History displays stored unit transaction records. It does not invent a posting/return timeline or infer a current accountable officer.
7. Documents selects an acquisition or unit and reuses CardUpload. Additional costs and their supporting documents remain available from the unit editor.

Registry pagination supports 12/20/40/80 records in both views. Acquisitions supports the same sizes. Unit grids use 12/20/40. Secondary records load when their tab and parent are selected.

Received sums original receipt rows only; transfer rows are not counted again as receipts. Balance and balance value reuse the existing transfer projection. Amount is the existing balance value, so the UI labels it accordingly rather than misrepresenting it as the original acquisition total.

## Preserved behavior and intentional limits

Existing master/acquisition/unit CRUD endpoints, access checks, posting validation, transfer service, Crystal reports, upload storage and additional-cost calculation remain authoritative. Posted acquisitions show Unpost and hide Transfer/Delete. Destructive and posting actions use Kendo confirmation.

The additional-cost formula remains TotalAddCost + UnitCost. Upload requests and document edit/delete models carry their acquisition parent, while ImageId retains the existing unit/acquisition-group key.

MVC5, EF6, Kendo, Bootstrap and _AppLayout remain in use. Stored procedures, reports, the data model and shared forms were not redesigned. The registry and existing unit services retain their stored-procedure retrieval behavior; this change does not claim SQL-level paging improvements for those procedures.

## JavaScript/CSS cleanup and defects

Inline scripts/styles and category templates were extracted from Index. Workspace callbacks replace nested-row DOM assumptions with explicit acquisition/transfer context. New styles are scoped to the workspace. Remaining shared legacy form callbacks/styles were retained for compatibility rather than broadly removed.

Addressed in the active workflow:
- Acquisition selection used acquisition Id where the grid key was TransferId.
- Nested-grid parent lookup did not work in a standalone workspace.
- Repeated detail-event registration could accumulate handlers.
- An obsolete custom unit delete handler referenced an undefined row variable; the active action now uses an explicit selected model.
- Document responses did not carry the transient acquisition ID needed by mutation validation; the workspace supplies it.
- Validation handlers assumed a popup editor existed, including after failed deletes.
- Regression tests caught and fixed a new selection recursion and missing iframe report parameters.

## Verification completed

- Actual modified controller, new service and view models compiled against repository assemblies using C# 7.1.
- MVC5 Razor generation and compilation checked the changed/new PropertyCard views against the installed Kendo assembly.
- JavaScript syntax checks passed.
- Headless Chrome tests used the installed Kendo scripts, actual registry/workspace JavaScript and synthetic data.
- Registry checks covered shared paging/page size, switching without another read, filter retention, empty state, clear filters, escaped text, desktop/mobile catalogue layout, tile Edit/Print targeting and Workspace navigation.
- Workspace checks covered lazy/cached tabs, posting ID and action visibility, document parent/upload payload, PAR/ICS parent, receipt semantics, acquisition and four category editor parent bindings, transfer ID, original/update report parameters, unchanged additional-cost calculation and failed-read recovery.

Browser requests were intercepted; no live records were posted, transferred, deleted or uploaded. These checks are not an authenticated end-to-end run against the application database, and do not validate stored-procedure execution or Crystal report rendering. A full application rebuild/deployment was not performed.

## Manual regression checklist

Use appropriate test records after building the project:

- [ ] Index: List/Catalogue, search, column filters, encoder filter, all page sizes, page navigation, empty results, Add/Edit/Delete, Print Selected Card and View Property Card.
- [ ] Overview: correct card identity/header, empty-card behavior, counts, latest PO, original receipts, transfers, issued quantity, balance and balance value against existing records.
- [ ] Acquisitions: paging/filtering, details, Add/Edit/Cancel, duplicate-card-to-acquisition flow, Post/Unpost, blocked deletes and transfers for posted records, successful permitted transfer, refreshed summaries.
- [ ] Reports: master Property Card plus Original and Update PO reports open the selected record and render correctly.
- [ ] Units: Vehicle, Other, Land and Building data, paging, existing Add/Edit/Delete rules, correct acquisition/transfer/location defaults, and restricted creation on transferred rows.
- [ ] Accountability: select units with and without PAR/ICS links; verify reference, officer and location data.
- [ ] History: stored records, sorting/filtering/paging, and empty-state behavior.
- [ ] Documents: both acquisition and unit parents; upload, preview, edit description, delete, posted/PAR validation and record labels; ensure no cross-parent attachment changes.
- [ ] Additional costs: add/edit/delete, resulting acquisition cost, supporting upload/preview and reopening the unit.
- [ ] Permissions and layout: restricted users, invalid/cross-card context IDs, server validation errors, narrow screens and repeated tab/modal use.
