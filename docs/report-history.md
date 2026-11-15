# Report history

`POST /api/Report` generates a PDF, saves its exact bytes and period title in PostgreSQL, and returns the PDF as before. Every successful generation creates a separate archive entry. A generation or archive write failure does not create a history entry or return a successful download.

Admin-only endpoints:

- `GET /api/Report/history?page=1` returns an `ApiSuccessResult<PagedResultDto<ReportHistoryDto>>`, newest first, ten entries per page. Metadata includes `id`, `periodTitle`, `createdAtUtc`, `fileName`, and `sizeBytes`; PDF bytes are excluded.
- `GET /api/Report/history/{id}` downloads the saved PDF, without regenerating it. Missing IDs return 404.

History is shared by administrators and survives browser reloads and device changes. Changing source data does not change archived files. Reports generated before this feature cannot be recovered retroactively.

Apply migration `20261115174500_AddReportHistory` before running the updated API. Development startup applies migrations automatically; production uses the existing migration bundle deployment step.
