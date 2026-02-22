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
   - **Name**: `cs366-form-demo` (this becomes `cs366-form-demo.azurewebsites.net`)
   - **Publish**: Code
   - **Runtime stack**: .NET 8 (LTS)
   - **Operating System**: Linux
   - **Region**: pick one close to you (e.g. West US 2)
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
the name you chose in step 3 above (e.g. `cs366-form-demo`).

### Trigger the Deploy

- **Automatic**: push any commit to the `main` branch
- **Manual**: go to the repo's **Actions** tab > **Deploy to Azure** > **Run workflow**

The workflow takes about 1-2 minutes. Once it completes, visit
`https://cs366-form-demo.azurewebsites.net` to see the running app.

---

## Part 3: Enable Application Logging

By default, Azure App Service does **not** capture application logs. You must
enable it.

1. In the Azure portal, open your App Service
2. In the left sidebar, scroll to **Monitoring** > **App Service logs**
3. Set the following:
   - **Application logging (Filesystem)**: **On**
   - **Level**: **Verbose** (this captures Trace and above — all levels)
   - **Quota (MB)**: 35 (default is fine)
   - **Retention Period (Days)**: 1 (enough for a demo)
4. Click **Save**

> **Important**: Filesystem logging auto-disables after 12 hours. This is by
> design — it's meant for short debugging sessions, not permanent monitoring.

---

## Part 4: View Logs in the Azure Portal

### Option A: Log Stream (real-time)

This is the easiest way to watch logs as they happen.

1. In your App Service, go to **Monitoring** > **Log stream**
2. You'll see a live feed of output — it may take 30-60 seconds to connect
3. Now open the app in another browser tab and click around:
   - Submit a search → see the `Information` log
   - Use the filter form → see the `Warning` about the space-in-name bug
   - Try the wrong login → see the `Error` log about credentials in the URL
   - Visit `/crash` → see the `Critical` log and the exception stack trace
4. Watch the log stream update in real time

### Option B: Log Files via Kudu (Advanced Tools)

For browsing historical log files:

1. In your App Service, go to **Development Tools** > **Advanced Tools** > click **Go →**
2. This opens the Kudu console at `https://cs366-form-demo.scm.azurewebsites.net`
3. Navigate to: **Debug console** > **Bash**
4. Browse to `/home/LogFiles/` — you'll find:
   - `stdout_*.log` — your application's console output
   - Files in the `Application/` folder — structured app logs

### Option C: Download Logs as a ZIP

1. Visit: `https://cs366-form-demo.scm.azurewebsites.net/api/dump`
2. This downloads a ZIP of all current log files

---

## Part 5: Understanding Log Levels

The app uses six standard .NET log levels. Here's what each one means and
where it appears in this demo:

| Level | When to use it | Example in this app |
|-------|---------------|---------------------|
| **Trace** | Ultra-detailed diagnostic info; usually off in production | Redirect from `/` to `/index.html` |
| **Debug** | Detailed info useful during development | Raw query strings, file upload details |
| **Information** | General operational events — "this happened" | Search terms, login attempts, form submissions |
| **Warning** | Something unexpected but not broken | Space-in-name bug, large file uploads |
| **Error** | Something failed that shouldn't have | Credentials sent via GET (security problem) |
| **Critical** | App is about to crash or is unusable | The `/crash` endpoint |

### Filtering by Level in Azure

In **App Service logs** settings, the **Level** dropdown controls the minimum
level captured:

| Setting | What gets logged |
|---------|-----------------|
| Verbose | Trace + Debug + Information + Warning + Error + Critical |
| Information | Information + Warning + Error + Critical |
| Warning | Warning + Error + Critical |
| Error | Error + Critical |

**Try it**: set the level to **Warning**, submit some forms, then check the
log stream — you'll only see the Warning, Error, and Critical messages. The
Information and Debug messages are filtered out.

---

## Part 6: Endpoints to Try

| URL | Method | What it logs |
|-----|--------|-------------|
| `/` | GET | Trace: redirect |
| `/search?search=kittens` | GET | Debug + Information: query details |
| `/filter?name=test&end+date=2026-01-01` | GET | Debug + Warning: space-in-name bug |
| `/login-wrong?username=admin&password=secret` | GET | Error: credentials in URL |
| `/login-right` (via form) | POST | Information: login attempt |
| `/submit` (via form) | POST | Information + Debug: form fields + file details |
| `/crash` | GET | Critical: intentional exception |

---

## Troubleshooting

- **"Log stream is connecting..."** — wait 30-60 seconds; try refreshing the page
- **No logs appearing** — verify Application logging is set to **On** and level is **Verbose**
- **Logs stop appearing** — filesystem logging auto-disables after 12 hours; re-enable it
- **Deploy failed** — check the GitHub Actions tab for error details; most common issue is the publish profile secret being incorrect or the app name not matching
