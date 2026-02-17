# FormDemo — HTML Forms & HTTP Methods

A minimal ASP.NET Core app that strips away MVC so you can see exactly what happens when a browser submits a form. No controllers, no models, no views, no Razor — just raw HTTP requests and responses.

## Running the Demo

```bash
dotnet run
```

Then open <http://localhost:5000> in your browser.

**Keep DevTools open** (F12 > Network tab) the entire time. This is where the real learning happens.

## What's Inside

The app has two files that matter:

| File | Purpose |
|------|---------|
| `wwwroot/index.html` | The front-end — five HTML forms on a single page |
| `Program.cs` | The back-end — Minimal API endpoints that echo back exactly what they received |

Every endpoint responds with a table showing the key-value pairs the server received, so you can connect what you typed in the form to what arrived on the server.

## The Five Demos

### 1. Simple Search (GET)

A single text input submitted via GET. Demonstrates:

- The `name` attribute becomes the query string key (`?search=kittens`)
- Data is visible in the URL bar
- `context.Request.Query["search"]` reads it on the server

### 2. Multiple Fields with a Bug (GET)

Four fields submitted via GET, with an **intentional mistake**: the "Ending Date" field has `name="end date"` (with a space). Demonstrates:

- Multiple fields become `?name=...&start_date=...&end+date=...&count=...`
- Spaces in `name` attributes get URL-encoded as `+` or `%20`
- Why you should always use underscores or camelCase in `name` attributes

### 3. Login the WRONG Way (GET)

Username and password submitted via GET. Demonstrates:

- The password appears in the URL bar for anyone to see
- It's saved in browser history
- It shows up in server access logs
- `type="password"` only masks the input on screen — it does nothing for transport

### 4. Login the RIGHT Way (POST)

The same login form, but using POST. Demonstrates:

- The password is **not** in the URL
- Data travels in the request body instead of the query string
- The server reads it with `ReadFormAsync()` instead of `Request.Query`
- You can see the payload in DevTools > Network > click the request > Payload
- POST still isn't encrypted — you need HTTPS for real security

### 5. File Upload (POST multipart)

A form with text fields and a file input, using `enctype="multipart/form-data"`. Demonstrates:

- Without `enctype="multipart/form-data"`, file uploads don't work (only the filename is sent)
- The `Content-Type` header changes from `application/x-www-form-urlencoded` to `multipart/form-data`
- The server reads files via `form.Files` and regular fields via `form["key"]`

## Key Takeaways

1. **The `name` attribute is everything.** It becomes the key in the key-value pair sent to the server. No `name` = the field doesn't get sent.
2. **GET puts data in the URL; POST puts it in the body.** Use GET for searches and filters; use POST for anything that creates, updates, or contains sensitive data.
3. **`type="password"` is cosmetic.** It hides characters on screen but has zero effect on how data is transmitted.
4. **File uploads require `enctype="multipart/form-data"`.** Without it, the browser only sends the filename string.
5. **This is what MVC does for you.** Model binding, `[HttpGet]`, `[HttpPost]`, and tag helpers all automate exactly what this demo does by hand.

## Things to Try

- Submit a form, then hit the browser's Back button and resubmit. Notice how GET requests can be bookmarked/refreshed, but POST triggers a "resubmit" warning.
- In Demo 2, fix the bug by changing `name="end date"` to `name="end_date"` and resubmit.
- In DevTools > Network, compare the **Headers** and **Payload** tabs between GET and POST requests.
- Try submitting Demo 5 without selecting a file to see what the server receives.
