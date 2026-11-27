# Verification Checklist – HubLink (PT5)

The team reports that all listed application and API checks passed in its local environment.
Checklist results below reflect that report. The CSV sample was also inspected in the submission
folder: it has the required header, five shipment rows, and correctly quoted comma/quote fields.

Execution date: 27/11/2026 | Service: teacher-provided local API | Testers: team members listed in README

| ID | How to check | Expected | Status | Notes / observed |
|---|---|---|---|---|
| AC-01 | Start service, run app with BR-001 | 5 shipments shown, values match browser JSON at `/api/shipments?branchCode=BR-001` | PASS | Team reports successful API retrieval; five BR-001 shipments displayed. |
| AC-02 | Select each status filter, then All | Only matching rows (Pending 1, InTransit 2, Delivered 1, Exception 1); All shows 5 | PASS | Team reports all status filters and All displayed the expected records. |
| AC-03 | Read summary panel | Total 5; Pending 1, InTransit 2, Delivered 1, Exception 1; unchanged when filter changes | PASS | Team reports expected counts and that filtering did not change the summary. |
| AC-04 | Click Refresh (try `mode=delay` with timeout 15 to see loading) | UI stays responsive, progress bar shows, buttons disabled while loading, data updates | PASS | Team reports loading and refresh behavior passed. |
| AC-05a | Close the service window, click Refresh | "Could not reach…" error, no success message, button reads Retry | PASS | Team reports unavailable-service handling passed; previous data behavior is covered by AC-05d. |
| AC-05b | `mode=delay`, timeout 8 | Timeout message, Retry available | PASS | Team reports timeout handling passed and retry was available. |
| AC-05c | `mode=bad-request` and `mode=server-error` | HTTP 400 / 500 message shown, no false success | PASS | Team reports both HTTP error scenarios passed. |
| AC-05d | Load OK, then stop service and Refresh | Old data stays, amber "previously loaded" banner shown | PASS | Team reports previously loaded data was retained and identified after a failed refresh. |
| AC-05e | Restart service, click Retry | Data reloads, error and amber banner cleared | PASS | Team reports retry after restoring service succeeded. |
| AC-06a | `mode=empty` or branch `BR-999` | "Returned no shipments" message (not an error); counts all 0 | PASS | Team reports BR-999 returned an empty result without an application error. |
| AC-06b | `mode=invalid-json` | "Data could not be used" error | PASS | Team reports invalid JSON was handled as an error. |
| AC-07 | Filter = InTransit, Export CSV, open file | Header `TrackingNumber,Origin,Destination,Status,LastUpdatedUtc`; exactly 2 rows; commas/quotes correctly quoted | PASS | Team reports filtered CSV export passed. The included All-filter sample has the exact five headers and five records; comma and quote fields are quoted. |
| AC-08 | Export to a read-only/nonexistent location or while file is open/locked | "Export failed" message, no success claim | PASS | Team reports CSV failure handling passed. |
| Extra | Export with filter showing 0 rows | "Nothing to export" message, no file | PASS | Team reports empty-filter export handling passed. |

## Remaining notes
- Results in this checklist are based on the team's report that all listed tests passed locally; this environment did not repeat the application tests.
- The application-exported sample is `Submission/Samples/shipments-BR-001-All-20261127-223837.csv`.
