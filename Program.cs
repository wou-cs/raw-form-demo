// ============================================================================
// FormDemo — Minimal API app for CS-366 Week 7
//
// NO controllers, NO models, NO views, NO Razor.
// Just raw HTTP request/response so you can see what MVC normally hides.
//
// Run:   dotnet run
// Open:  http://localhost:5000
// ============================================================================

using System.Net;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Logging configuration — Azure App Service picks up these settings.
// By default, Information and above are logged. We lower it to Trace so
// students can experiment with filtering in the Azure portal.
// ---------------------------------------------------------------------------
builder.Logging.SetMinimumLevel(LogLevel.Trace);

var app = builder.Build();

// Get a logger we can use in our minimal API endpoints
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("FormDemo");

// Serve static files from wwwroot/ (our index.html lives there)
app.UseStaticFiles();

// ---------------------------------------------------------------------------
// Helper: wraps any HTML body content in a Bootstrap page so responses
// look decent without us hand-writing full HTML each time.
// ---------------------------------------------------------------------------
string WrapInPage(string title, string bodyContent)
{
    return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{HtmlEncode(title)}</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css""
          rel=""stylesheet"">
</head>
<body>
    <nav class=""navbar navbar-dark bg-dark mb-4"">
        <div class=""container"">
            <a class=""navbar-brand"" href=""/"">Form Demo</a>
        </div>
    </nav>
    <div class=""container"">
        {bodyContent}
        <hr class=""my-4"">
        <p><a href=""/"" class=""btn btn-outline-secondary btn-sm"">&larr; Back to forms</a></p>
    </div>
</body>
</html>";
}

// ---------------------------------------------------------------------------
// Helper: HTML-encode a string to prevent XSS.
// Any user input we echo back MUST go through this.
// ---------------------------------------------------------------------------
string HtmlEncode(string? value)
{
    return WebUtility.HtmlEncode(value ?? "(empty)");
}

// ---------------------------------------------------------------------------
// Helper: builds an HTML table of key-value pairs.
// Used by every endpoint to show exactly what the server received.
// ---------------------------------------------------------------------------
string BuildTable(IEnumerable<KeyValuePair<string, string?>> pairs)
{
    var rows = "";
    foreach (var pair in pairs)
    {
        rows += $@"
            <tr>
                <td><code>{HtmlEncode(pair.Key)}</code></td>
                <td>{HtmlEncode(pair.Value)}</td>
            </tr>";
    }

    if (rows == "")
    {
        rows = @"<tr><td colspan=""2"" class=""text-muted"">No data received</td></tr>";
    }

    return $@"
        <table class=""table table-bordered table-striped"">
            <thead class=""table-dark"">
                <tr><th>Name (key)</th><th>Value</th></tr>
            </thead>
            <tbody>{rows}</tbody>
        </table>";
}

// ============================================================================
// GET / — Redirect to the static index.html page
// ============================================================================
app.MapGet("/", () =>
{
    logger.LogTrace("Redirecting from / to /index.html");
    return Results.Redirect("/index.html");
});

// ============================================================================
// GET /search — Simple single-field query string demo
//
// The form sends:  GET /search?search=kittens
// We read it with: context.Request.Query["search"]
// ============================================================================
app.MapGet("/search", (HttpContext context) =>
{
    // Read the "search" key from the query string.
    // This key matches the name="search" attribute in the HTML form.
    string? searchTerm = context.Request.Query["search"];

    logger.LogDebug("GET /search hit with query string: {QueryString}", context.Request.QueryString.Value);
    logger.LogInformation("Search requested for term: {SearchTerm}", searchTerm ?? "(none)");

    var html = $@"
        <h2>GET /search</h2>
        <div class=""alert alert-info"">
            <strong>Method:</strong> GET<br>
            <strong>Path:</strong> /search<br>
            <strong>Raw query string:</strong> <code>{HtmlEncode(context.Request.QueryString.Value)}</code>
        </div>
        <h4>What the server received:</h4>
        {BuildTable(context.Request.Query.Select(q =>
            new KeyValuePair<string, string?>(q.Key, q.Value)))}
        <p class=""text-muted"">
            Look at the URL bar &mdash; the search term is right there in the URL.
            This is how GET works: data travels in the query string.
        </p>";

    return Results.Content(WrapInPage("GET /search", html), "text/html");
});

// ============================================================================
// GET /filter — Multiple fields via GET, including an intentional bug
//
// The form has name="end date" (with a space!) to demonstrate what happens
// when name attributes contain spaces.
// ============================================================================
app.MapGet("/filter", (HttpContext context) =>
{
    var paramCount = context.Request.Query.Count;
    logger.LogDebug("GET /filter hit with {ParamCount} query parameters", paramCount);

    // Log a warning when the space-in-name bug is present
    if (context.Request.Query.ContainsKey("end date"))
    {
        logger.LogWarning("Query parameter 'end date' contains a space — this is the intentional bug demo");
    }

    var html = $@"
        <h2>GET /filter</h2>
        <div class=""alert alert-info"">
            <strong>Method:</strong> GET<br>
            <strong>Path:</strong> /filter<br>
            <strong>Raw query string:</strong> <code>{HtmlEncode(context.Request.QueryString.Value)}</code>
        </div>
        <h4>What the server received:</h4>
        {BuildTable(context.Request.Query.Select(q =>
            new KeyValuePair<string, string?>(q.Key, q.Value)))}
        <div class=""alert alert-warning"">
            <strong>Notice the ""end date"" key?</strong> The space in the name attribute
            becomes <code>end+date</code> or <code>end%20date</code> in the URL.
            On the server, you'd need <code>Query[""end date""]</code> to read it.
            <br><br>
            <strong>Lesson:</strong> Use underscores or camelCase in name attributes:
            <code>name=""end_date""</code> or <code>name=""endDate""</code>.
        </div>";

    return Results.Content(WrapInPage("GET /filter", html), "text/html");
});

// ============================================================================
// GET /login-wrong — Password visible in the URL!
//
// This intentionally shows why you should NEVER use GET for sensitive data.
// The password appears in the URL bar, browser history, and server logs.
// ============================================================================
app.MapGet("/login-wrong", (HttpContext context) =>
{
    string? username = context.Request.Query["username"];
    string? password = context.Request.Query["password"];

    // ERROR-level: this is genuinely bad — credentials in a GET request
    logger.LogError("SECURITY: Login attempt via GET — credentials exposed in URL! User: {Username}", username);
    logger.LogDebug("GET /login-wrong full URL: {Path}{Query}", context.Request.Path, context.Request.QueryString.Value);

    var html = $@"
        <h2>GET /login-wrong</h2>
        <div class=""alert alert-danger"">
            <strong>SECURITY PROBLEM!</strong><br>
            <strong>Method:</strong> GET<br>
            <strong>Full URL:</strong> <code>{HtmlEncode(context.Request.Path + context.Request.QueryString.Value)}</code><br>
            <br>
            Look at the URL bar &mdash; the password is <strong>right there</strong>!<br>
            It's also now in your browser history. And the server's access logs.
        </div>
        <h4>What the server received:</h4>
        {BuildTable(context.Request.Query.Select(q =>
            new KeyValuePair<string, string?>(q.Key, q.Value)))}
        <p class=""text-muted"">
            The <code>type=""password""</code> attribute only masks the input box on screen.
            It does <strong>nothing</strong> to protect the data in transit when using GET.
        </p>";

    return Results.Content(WrapInPage("GET /login-wrong", html), "text/html");
});

// ============================================================================
// POST /login-right — Password in the request body (correct approach)
//
// Same form data, but now it travels in the request body instead of the URL.
// Open DevTools > Network tab to see the difference.
// ============================================================================
app.MapPost("/login-right", async (HttpContext context) =>
{
    // For POST, we read the form body instead of the query string.
    // This is what MVC's model binding does behind the scenes.
    var form = await context.Request.ReadFormAsync();

    string? username = form["username"];
    string? password = form["password"];

    logger.LogInformation("POST /login-right — login attempt for user: {Username}", username);
    // Note: we NEVER log the password, even at Debug level!

    var html = $@"
        <h2>POST /login-right</h2>
        <div class=""alert alert-success"">
            <strong>Method:</strong> POST<br>
            <strong>Path:</strong> /login-right<br>
            <strong>Content-Type:</strong> <code>{HtmlEncode(context.Request.ContentType)}</code><br>
            <br>
            Look at the URL bar &mdash; no password! The data is in the request body.
            Open DevTools &gt; Network &gt; click the request &gt; Payload to see it.
        </div>
        <h4>What the server received:</h4>
        {BuildTable(form.Select(f =>
            new KeyValuePair<string, string?>(f.Key, f.Value)))}
        <p class=""text-muted"">
            <strong>Important:</strong> POST doesn't encrypt anything &mdash; you still need
            HTTPS for real security. But at minimum the data isn't in the URL bar or browser history.
        </p>";

    return Results.Content(WrapInPage("POST /login-right", html), "text/html");
});

// ============================================================================
// POST /submit — Multi-field POST with file upload support
//
// Demonstrates enctype="multipart/form-data" for file uploads.
// Shows how the server reads both regular fields and uploaded files.
// ============================================================================
app.MapPost("/submit", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();

    logger.LogInformation("POST /submit — received {FieldCount} form fields, {FileCount} file(s)",
        form.Count, form.Files.Count);

    foreach (var file in form.Files)
    {
        logger.LogDebug("Uploaded file: {FileName}, Size: {FileSize} bytes, ContentType: {ContentType}",
            file.FileName, file.Length, file.ContentType);

        if (file.Length > 5_000_000)
        {
            logger.LogWarning("Large file upload: {FileName} is {FileSizeMB:F1} MB",
                file.FileName, file.Length / 1_000_000.0);
        }
    }

    // Build the table for regular form fields
    var fieldPairs = form
        .Where(f => f.Key != "file") // skip the file field, we handle it separately
        .Select(f => new KeyValuePair<string, string?>(f.Key, f.Value));

    // Check for uploaded files
    var fileInfo = "";
    if (form.Files.Count > 0)
    {
        foreach (var file in form.Files)
        {
            fileInfo += $@"
                <tr>
                    <td><code>{HtmlEncode(file.Name)}</code> (file)</td>
                    <td>
                        <strong>Filename:</strong> {HtmlEncode(file.FileName)}<br>
                        <strong>Size:</strong> {file.Length} bytes<br>
                        <strong>Content-Type:</strong> {HtmlEncode(file.ContentType)}
                    </td>
                </tr>";
        }
    }
    else
    {
        fileInfo = @"<tr><td colspan=""2"" class=""text-muted"">No file uploaded</td></tr>";
    }

    var html = $@"
        <h2>POST /submit</h2>
        <div class=""alert alert-success"">
            <strong>Method:</strong> POST<br>
            <strong>Path:</strong> /submit<br>
            <strong>Content-Type:</strong> <code>{HtmlEncode(context.Request.ContentType)}</code><br>
            <br>
            Notice the Content-Type is <code>multipart/form-data</code> &mdash; this is
            what <code>enctype=""multipart/form-data""</code> does. It's required for file uploads.
        </div>
        <h4>Form fields received:</h4>
        {BuildTable(fieldPairs)}
        <h4>Files received:</h4>
        <table class=""table table-bordered table-striped"">
            <thead class=""table-dark"">
                <tr><th>Name (key)</th><th>File details</th></tr>
            </thead>
            <tbody>{fileInfo}</tbody>
        </table>";

    return Results.Content(WrapInPage("POST /submit", html), "text/html");
});

// ============================================================================
// GET /crash — Demonstrates exception logging (Critical level)
//
// Throws an intentional exception so students can see how errors appear
// in Azure's log stream.
// ============================================================================
app.MapGet("/crash", () =>
{
    logger.LogCritical("CRITICAL: The /crash endpoint was hit! Something is very wrong.");
    logger.LogError("Error details: simulated system failure on /crash");
    logger.LogWarning("This is the last warning before the system goes down");
    return Results.Content(
        "<h1>500 - Internal Server Error</h1><p>Something went terribly wrong (on purpose).</p>",
        "text/html",
        statusCode: 500);
});

// Log at startup so students can see the app come online in the log stream
logger.LogInformation("FormDemo app starting — logging demo is active");

// Start the server
app.Run();
