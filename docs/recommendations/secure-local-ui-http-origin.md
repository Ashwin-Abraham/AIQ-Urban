# Securing the Local UI-to-Rhino HTTP Boundary

## Status and scope

This is a deferred security recommendation. It is not part of the `GetInstallationId` message implementation.

AIQ Urban serves its packaged UI from `http://localhost:6300/`, and the UI posts messages back to that same HTTP server. The server currently adds `Access-Control-Allow-Origin: *` to every response. Once the server exposes a stable Installation ID, an arbitrary website opened by the user could attempt to contact the localhost endpoint while AIQ Urban is running. The same boundary also carries model-changing commands, so this should ultimately be addressed for the whole POST API rather than only for `GetInstallationId`.

## Recommendation

Use a combined UI-and-Rhino approach:

1. Make production UI requests same-origin.
2. Enforce trusted origins in the Rhino HTTP server before processing a request.
3. Permit cross-origin access only for explicitly configured local development origins.
4. Consider a per-UI-session token as a later defence-in-depth improvement.

The enforcement must live in Rhino. UI-only checks cannot protect the endpoint because an untrusted webpage can bypass the AIQ Urban UI and call the localhost server directly.

## UI changes

The production UI should post to a relative URL such as `/` rather than the hard-coded `http://localhost:6300/`. Because `index.html` is served by `HttpUiServer`, a relative URL naturally targets the same scheme, host, and port.

`RhinoApiService` should obtain its base URL from one of two sources:

- Production: a relative same-origin URL.
- Local development: an explicit environment setting such as `VITE_RHINO_API_URL=http://localhost:6300/`.

The development URL must not be baked into the production bundle as a reason to retain wildcard CORS.

The UI should continue treating `InstallationIdMessage` as data and `ErrorMessage` as failure. The Installation ID is an identifier, not an authentication secret, and the UI must not use possession of it as proof that a caller is authorised.

## Rhino changes

`HttpUiServer` should validate the request's `Origin` before parsing or executing any `POST` message. This matters because CORS response headers alone primarily govern whether browser code may read a response; origin validation also prevents an untrusted browser origin from triggering model-changing commands.

Recommended policy:

- Allow the production origin served by the active server, normally `http://localhost:6300`.
- Allow a small, explicit set of development origins only in a development configuration.
- Reject every other supplied origin with HTTP 403 before calling `MessageConverter` or `HandleMessage()`.
- Do not use suffix, substring, or wildcard matching for origins. Compare normalized scheme, host, and port exactly.
- Replace `Access-Control-Allow-Origin: *` with the exact allowed origin on cross-origin development responses.
- Add `Vary: Origin` whenever the response varies by origin.
- Apply the same policy to preflight `OPTIONS` requests.

The behavior for requests without an `Origin` header needs an explicit policy. A pragmatic first pass is to allow them for existing non-browser localhost clients while continuing to bind only to the loopback interface. If the endpoint later becomes browser-only, require an allowed origin instead.

Origin validation should be centralized in `HttpUiServer`; individual messages such as `GetInstallationIdMessage` should not implement their own allowlists.

## Optional session-token hardening

Origin checks protect the browser boundary but are not authentication: a native process can omit or forge an `Origin` header. If the localhost API needs a stronger trust boundary, generate an unpredictable token when the Rhino UI session starts and require it on every UI POST.

A suitable flow is:

1. Rhino generates a random, short-lived session token.
2. The packaged UI receives it through a same-origin bootstrap mechanism.
3. `RhinoApiService` sends it in a dedicated request header.
4. `HttpUiServer` validates it before deserializing the message.
5. Closing the UI invalidates the token.

Do not reuse the Installation ID as this token. The Installation ID is stable and non-secret; the session token must be temporary and unpredictable.

## Websocket follow-up

The UI websocket is a separate browser-accessible localhost boundary. Its connection-origin and session-authentication behavior should be reviewed alongside the HTTP hardening so an untrusted page cannot subscribe to model or selection messages. This is related work, but it should not be silently folded into the Installation ID feature.

## Verification scenarios

- The packaged UI served from `http://localhost:6300` can post every supported message.
- An explicitly configured local development UI can post messages and complete preflight requests.
- A request carrying an untrusted web origin receives HTTP 403 and no handler executes.
- An untrusted origin cannot retrieve `InstallationIdMessage`.
- An untrusted origin cannot trigger a model-changing message.
- Existing approved non-browser localhost clients behave according to the chosen missing-`Origin` policy.
- If session tokens are added, missing, incorrect, expired, and previous-session tokens are rejected.

## Rollout

Implement relative UI URLs first, then add Rhino origin enforcement and remove wildcard CORS in the same release. Keeping those changes together avoids breaking the development UI while closing the production exposure. Treat session-token authentication and websocket hardening as follow-up work unless the localhost API is considered a security boundary against other local processes.
