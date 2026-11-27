# HubLink – Regional Logistics Data Exchange (ITSD81 Performance Task 5)

Avalonia (MVVM) desktop client that retrieves shipments from the HubLink REST API,
filters/summarises them, and exports the displayed records to CSV.

**Team members:** 
Barazan, Brill Jarn T. - 2022303102
Vergel de Dios, Donne Travis S. - 2024300465

## Requirements
- .NET SDK 8.0 or newer (`dotnet --version`)
- The HubLink local service running (see `START-HERE` in the service ZIP)

## Run
1. Extract the service ZIP, double-click `Start-HubLink.cmd`, keep that window open
   (use `Start-HubLink-5081.cmd` if port 5080 is busy).
2. In this project folder, restore, build, and run:
   ```
   dotnet restore HubLink/HubLink.csproj
   dotnet build HubLink/HubLink.csproj
   dotnet run --project HubLink/HubLink.csproj
   ```
   Data loads automatically on start; use **Refresh / Retry** to reload.

## Using the application
- Choose **All** or a shipment status in **Status filter** to filter the displayed rows. Summary counts always cover all retrieved shipments.
- Select **Export CSV…** to save the currently displayed rows. The CSV uses the required five columns and quotes fields containing commas, quotes, or newlines.
- A successful empty response (for example, branch `BR-999`) is shown as an empty result. Refresh errors provide a retry action and retain previously loaded data when available.

## Configuration (no code changes needed)
Edit `HubLink/appsettings.json` (copied next to the executable on build):

| Key | Default | Meaning |
|---|---|---|
| `BaseUrl` | `http://127.0.0.1:5080` | Service address (use `5081` if you started that instance, or the instructor's LAN address) |
| `BranchCode` | `BR-001` | Branch to retrieve (`BR-001`, `BR-002`, `BR-003`, `BR-999` = empty) |
| `EndpointPath` | `/api/shipments` | Request path |
| `ExtraQuery` | `""` | Extra query string, e.g. `mode=server-error` |
| `TimeoutSeconds` | `8` | Client timeout (must be < 10 to time out `mode=delay`) |

Environment variables override the file: `HUBLINK_BASE_URL`, `HUBLINK_BRANCH_CODE`,
`HUBLINK_ENDPOINT_PATH`, `HUBLINK_EXTRA_QUERY`, `HUBLINK_TIMEOUT_SECONDS`.

**Testing failures:** set `EndpointPath` to `/api/test/shipments` and `ExtraQuery` to
`mode=bad-request`, `server-error`, `invalid-json`, `empty` or `delay`, then restart the app.
Restore `/api/shipments` and an empty `ExtraQuery` afterwards. For "service unreachable",
close the service window and press Refresh.

## Structure
- `Models/` – `Shipment`, `ShipmentStatus`, `ShipmentResult`
- `Services/` – `ShipmentApiClient` (HttpClient + JSON parsing/validation), `CsvExporter`, `AppSettings`
- `ViewModels/` – `MainViewModel` (state, filtering, summary, commands)
- `Views/` – `MainWindow` (XAML; code-behind only opens the save dialog)

## AI assistance acknowledgement
The initial project source and documentation were drafted with AI assistance (Claude, Anthropic).
For this submission, the default API URL is set to port 5080, required Origin and Destination values are checked for blank content, and the team reports that the verification checks in `VERIFICATION-CHECKLIST.md` passed locally. AI assistance was also used to update the checklist and submission documentation.

## Submission status
The generated `bin/` and `obj/` build folders are ignored and should be excluded from a source ZIP.
An application-exported BR-001 sample is included at `Submission/Samples/shipments-BR-001-All-20261127-223837.csv`.
