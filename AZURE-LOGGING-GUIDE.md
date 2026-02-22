# Azure Deployment & Logging Guide — FormDemo

This guide walks through deploying the FormDemo app to Azure App Service and
using the Azure portal to view application logs at different levels.

---

## Part 1: Create the Azure App Service

1. Go to the [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** > **Web App**
3. Fill in the basics:
   - **Subscription**: your Azure for Education (or whichever you use)
   - **Resource Group**: create new, e.g. `cs366-demos`
   - **Name**: `wou-learn-logging` (this becomes `wou-learn-logging.azurewebsites.net`)
   - **Publish**: Code
   - **Runtime stack**: .NET 8 (LTS)
   - **Operating System**: Linux
   - **Region**: pick one close to you (e.g. Canada Central)
   - **Pricing plan**: Free F1 (sufficient for demos)
4. Click **Review + create**, then **Create**
5. Wait for deployment to complete (~1 minute)

---

## Part 2: Connect GitHub Actions for Deployment

### Get the Publish Profile

1. In the Azure portal, open your new App Service
2. On the **Overview** page, click **Download publish profile** (top toolbar)
3. This downloads a `.PublishSettings` XML file — open it in a text editor and
   copy the entire contents

### Add the Secret to GitHub

1. Go to the GitHub repo: https://github.com/wou-cs/raw-form-demo
2. **Settings** > **Secrets and variables** > **Actions**
3. Click **New repository secret**
   - **Name**: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - **Value**: paste the entire `.PublishSettings` XML content
4. Click **Add secret**

### Update the Workflow (if needed)

In `.github/workflows/azure-deploy.yml`, make sure `AZURE_WEBAPP_NAME` matches
the name you chose in step 1 above.

### Trigger the Deploy

- **Automatic**: push any commit to the `main` branch
- **Manual**: go to the repo's **Actions** tab > **Deploy to Azure** > **Run workflow**

The workflow takes about 1-2 minutes.

---

## Part 3: Enable Application Logging

By default, Azure App Service does **not** capture application logs. You must
enable it.

1. In the Azure portal, open your App Service
2. In the left sidebar, scroll to **Monitoring** > **App Service logs**
3. Set the following:
   - **Application logging (Filesystem)**: **On**
   - **Level**: **Verbose** (leave this on Verbose — we'll control filtering a better way)
   - **Quota (MB)**: 35 (default is fine)
   - **Retention Period (Days)**: 1 (enough for a demo)
4. Click **Save**

> **Important**: Filesystem logging auto-disables after 12 hours. This is by
> design — it's meant for short debugging sessions, not permanent monitoring.

> **Note**: On Linux App Service, the "Level" dropdown in App Service logs and
> the Log Stream filter **do not reliably filter output**. The Log Stream reads
> raw stdout from the container. To control what gets logged, we change the
> app's own configuration — see Part 5.

---

## Part 4: View Logs — Log Stream (real-time)

This is the easiest way to watch logs as they happen.

1. In your App Service, go to **Monitoring** > **Log stream**
2. You'll see a live feed of output — it may take 30-60 seconds to connect
3. Now open the app in another browser tab and click around:
   - Submit a search → see `info: FormDemo` and `dbug: FormDemo` messages
   - Use the filter form → see `warn: FormDemo` about the space-in-name bug
   - Try the wrong login → see `fail: FormDemo` about credentials in the URL
   - Visit `/crash` → see `crit:`, `fail:`, and `warn:` all at once
4. Watch the log stream update in real time

You can also browse historical log files:

1. In your App Service, go to **Development Tools** > **Advanced Tools** > click **Go →**
2. This opens the Kudu console
3. Navigate to: **Debug console** > **Bash**
4. Browse to `/home/LogFiles/` — look for `stdout_*.log` files

---

## Part 5: Controlling Log Levels (the key demo!)

The app reads its log level from `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

- `"Default": "Trace"` — our FormDemo logger emits everything (Trace and above)
- `"Microsoft.AspNetCore": "Warning"` — suppresses the noisy framework logs

### Changing the level live in Azure (no redeploy!)

.NET configuration supports **environment variable overrides**. In Azure:

1. Open your App Service
2. Go to **Settings** > **Environment variables** (or **Configuration** > **Application settings**)
3. Click **+ Add**
4. Set:
   - **Name**: `Logging__LogLevel__Default`
   - **Value**: `Warning`
5. Click **Apply**, then **Apply** again to confirm restart

> The double-underscore (`__`) is how .NET reads nested JSON keys from
> environment variables. `Logging__LogLevel__Default` overrides
> `Logging.LogLevel.Default` in appsettings.json.

Now go back to the **Log Stream** and submit forms. You'll only see Warning,
Error, and Critical messages from FormDemo — the Information and Debug messages
are gone.

### Try different levels

| Value | What FormDemo logs you'll see |
|-------|------------------------------|
| `Trace` | Everything (Trace, Debug, Info, Warning, Error, Critical) |
| `Information` | Info, Warning, Error, Critical |
| `Warning` | Warning, Error, Critical |
| `Error` | Error, Critical only |

**To reset**: delete the `Logging__LogLevel__Default` environment variable and
the app falls back to what's in appsettings.json (`Trace`).

---

## Part 6: Understanding Log Levels

The app uses six standard .NET log levels. Here's what each one means and
where it appears in this demo:

| Level | Log prefix | When to use it | Example in this app |
|-------|-----------|----------------|---------------------|
| **Trace** | `trce:` | Ultra-detailed diagnostic info; usually off in production | Redirect from `/` to `/index.html` |
| **Debug** | `dbug:` | Detailed info useful during development | Raw query strings, file upload details |
| **Information** | `info:` | General operational events — "this happened" | Search terms, login attempts, form submissions |
| **Warning** | `warn:` | Something unexpected but not broken | Space-in-name bug, large file uploads |
| **Error** | `fail:` | Something failed that shouldn't have | Credentials sent via GET (security problem) |
| **Critical** | `crit:` | App is about to crash or is unusable | The `/crash` endpoint |

---

## Part 7: Endpoints to Try

| URL | Method | What it logs |
|-----|--------|-------------|
| `/` | GET | Trace: redirect |
| `/search?search=kittens` | GET | Debug + Information: query details |
| `/filter?name=test&end+date=2026-01-01` | GET | Debug + Warning: space-in-name bug |
| `/login-wrong?username=admin&password=secret` | GET | Error: credentials in URL |
| `/login-right` (via form) | POST | Information: login attempt |
| `/submit` (via form) | POST | Information + Debug: form fields + file details |
| `/crash` | GET | Critical + Error + Warning: simulated failure |

---

## Troubleshooting

- **"Log stream is connecting..."** — wait 30-60 seconds; try refreshing the page
- **No logs appearing** — verify Application logging is set to **On** and level is **Verbose**
- **Too much framework noise** — the `"Microsoft.AspNetCore": "Warning"` setting in appsettings.json suppresses most of it; if you still see too much, add `Logging__LogLevel__Microsoft.AspNetCore=None` as an env var
- **Logs stop appearing** — filesystem logging auto-disables after 12 hours; re-enable it
- **Deploy failed** — check the GitHub Actions tab for error details; most common issue is the publish profile secret being incorrect or the app name not matching
- **Env var change not taking effect** — Azure restarts the app when you save env vars; wait ~30 seconds for the restart to complete
