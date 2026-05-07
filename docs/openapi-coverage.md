# OpenAPI Coverage

This repository currently targets a deliberate subset of the Opencode server API.

That surface is smaller than the current server API documented at `https://opencode.ai/docs/es/server/`.

## How to run the server

Start a standalone server with:

```bash
opencode serve --hostname 127.0.0.1 --port 4096
```

Base URL:

```text
http://127.0.0.1:4096
```

OpenAPI document:

```text
http://127.0.0.1:4096/doc
```

If you launch the interactive `opencode` client, it also starts a local server. In that mode this SDK defaults to `http://localhost:54321`, so the OpenAPI document is typically available at `http://localhost:54321/doc`.

## Coverage summary

Implemented in this SDK:

- `GET /event`
- `GET /app`
- `POST /app/init`
- `POST /log`
- `GET /mode`
- `GET /config/providers`
- `GET /config`
- `GET /find`
- `GET /find/file`
- `GET /find/symbol`
- `GET /file`
- `GET /file/status`
- `GET /session`
- `POST /session`
- `DELETE /session/{id}`
- `POST /session/{id}/abort`
- `POST /session/{id}/init`
- `GET /session/{id}/message`
- `POST /session/{id}/message`
- `POST /session/{id}/revert`
- `POST /session/{id}/share`
- `DELETE /session/{id}/share`
- `POST /session/{id}/summarize`
- `POST /session/{id}/unrevert`
- `POST /tui/append-prompt`
- `POST /tui/open-help`

Not implemented from the current server documentation:

- Global endpoints: `/global/health`, `/global/event`
- Project endpoints: `/project`, `/project/current`
- Path and VCS endpoints: `/path`, `/vcs`
- Instance endpoint: `/instance/dispose`
- Config mutation: `PATCH /config`
- Provider and auth endpoints: `/provider`, `/provider/auth`, `/provider/{id}/oauth/authorize`, `/provider/{id}/oauth/callback`, `PUT /auth/:id`
- Additional session endpoints: `/session/status`, `GET /session/:id`, `PATCH /session/:id`, `/session/:id/children`, `/session/:id/todo`, `/session/:id/fork`, `/session/:id/diff`, `/session/:id/permissions/:permissionID`
- Additional message endpoints: `GET /session/:id/message/:messageID`, `POST /session/:id/prompt_async`, `POST /session/:id/command`, `POST /session/:id/shell`
- Command catalog: `GET /command`
- Additional file surface documented by the server: `/file/content`
- Experimental tools: `/experimental/tool/ids`, `/experimental/tool`
- Runtime services: `/lsp`, `/formatter`, `/mcp`
- Agent catalog: `/agent`
- Additional TUI endpoints: `/tui/open-sessions`, `/tui/open-themes`, `/tui/open-models`, `/tui/submit-prompt`, `/tui/clear-prompt`, `/tui/execute-command`, `/tui/show-toast`, `/tui/control/next`, `/tui/control/response`
- Documentation endpoint wrapper: `/doc`

## Interpretation

At the moment, the .NET SDK is complete with respect to the subset of endpoints intentionally implemented in this repository.

It is not complete with respect to the broader server API documented publicly for the current Opencode server.

When adding new client areas, prefer using the server's `/doc` endpoint as the source of truth for the live server instance you want to support.