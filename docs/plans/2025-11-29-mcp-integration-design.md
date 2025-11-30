# MCP Integration Design for Basic-10

**Date:** 2025-11-29
**Version:** 1.0
**Status:** Approved

---

## Overview

Add MCP (Model Context Protocol) integration to Basic-10, allowing Claude Code to directly interact with the IDE. This includes a chat window for seamless collaboration without switching between windows.

## Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                        Basic-10 IDE                             │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │ BASIC Editor │  │  IC10 Output │  │   Claude Chat Window  │ │
│  │              │  │              │  │   (Pop-out, non-modal)│ │
│  └──────────────┘  └──────────────┘  └───────────────────────┘ │
│                            │                      │             │
│                    ┌───────┴──────────────────────┘             │
│                    ▼                                            │
│            ┌──────────────────┐                                 │
│            │  HTTP API Server │ (localhost:19410)               │
│            │  - GET/POST code │                                 │
│            │  - Compile       │                                 │
│            │  - Errors        │                                 │
│            │  - Symbols       │                                 │
│            │  - Devices       │                                 │
│            │  - Chat bridge   │                                 │
│            └────────┬─────────┘                                 │
└─────────────────────┼───────────────────────────────────────────┘
                      │ HTTP
                      ▼
            ┌──────────────────┐
            │ Basic10.Mcp.exe  │ (Spawned by Claude Code)
            │  - stdio ↔ HTTP  │
            │  - Chat routing  │
            └────────┬─────────┘
                     │ stdio (JSON-RPC)
                     ▼
            ┌──────────────────┐
            │   Claude Code    │ (User's Max subscription)
            └──────────────────┘
```

**Key points:**
- Basic-10 runs an HTTP server on `localhost:19410`
- `Basic10.Mcp.exe` is a small bridge that Claude Code spawns
- Chat messages flow bidirectionally through MCP
- All editor operations use the HTTP API
- No separate API key needed - routes through Claude Code

---

## HTTP API Endpoints

### Code Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/code` | Get current BASIC source from editor |
| `POST` | `/api/code` | Set BASIC source (replaces editor content) |
| `PATCH` | `/api/code` | Insert/modify at cursor or line range |

### Compilation

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/compile` | Compile current code, return IC10 output |
| `GET` | `/api/ic10` | Get current IC10 output |
| `GET` | `/api/errors` | Get compilation errors/warnings |

### Symbol Table

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/symbols` | Get all variables, labels, aliases, constants |
| `GET` | `/api/symbols/variables` | Just variables |
| `GET` | `/api/symbols/labels` | Just labels |

### Device Database

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/devices` | List all devices (with optional search) |
| `GET` | `/api/devices/{hash}` | Get device by hash |
| `GET` | `/api/properties` | List all logic properties |

### Chat Bridge

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/chat/send` | Send message to Claude Code |
| `GET` | `/api/chat/messages` | Get conversation history |
| `WebSocket` | `/api/chat/stream` | Real-time message streaming |

---

## MCP Tools

The `Basic10.Mcp.exe` exposes these tools to Claude Code:

```json
{
  "tools": [
    {
      "name": "basic10_get_code",
      "description": "Get the current BASIC source code from the editor",
      "inputSchema": { "type": "object", "properties": {} }
    },
    {
      "name": "basic10_set_code",
      "description": "Replace the editor content with new BASIC code",
      "inputSchema": {
        "type": "object",
        "properties": {
          "code": { "type": "string", "description": "The BASIC source code" }
        },
        "required": ["code"]
      }
    },
    {
      "name": "basic10_compile",
      "description": "Compile the current code and return IC10 output with any errors",
      "inputSchema": { "type": "object", "properties": {} }
    },
    {
      "name": "basic10_get_symbols",
      "description": "Get all defined variables, labels, aliases, and constants",
      "inputSchema": { "type": "object", "properties": {} }
    },
    {
      "name": "basic10_lookup_device",
      "description": "Search for device by name or get device by hash",
      "inputSchema": {
        "type": "object",
        "properties": {
          "query": { "type": "string", "description": "Device name to search" },
          "hash": { "type": "integer", "description": "Device hash (optional)" }
        }
      }
    },
    {
      "name": "basic10_get_properties",
      "description": "Get list of all device logic properties",
      "inputSchema": { "type": "object", "properties": {} }
    }
  ]
}
```

---

## Chat Window UI

Pop-out window for interacting with Claude Code from within Basic-10:

```
┌─────────────────────────────────────────────────────────┐
│ Claude Assistant                             [─] [□] [×]│
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ● Connected to Claude Code                          │ │
│ └─────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────┐ │
│ │  [Message history with timestamps]                  │ │
│ │  Shows when Claude is making editor changes         │ │
│ └─────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────┤
│ ┌───────────────────────────────────────────┐ ┌──────┐ │
│ │ Type a message...                         │ │ Send │ │
│ └───────────────────────────────────────────┘ └──────┘ │
└─────────────────────────────────────────────────────────┘
```

**Features:**
- Non-modal (can interact with editor while open)
- Connection status indicator
- Message history with timestamps
- Shows when Claude is making editor changes
- Send with Enter key or button
- Resizable, remembers position

**Access via:**
- Menu: View → Claude Assistant
- Keyboard: F10 or Ctrl+Shift+C
- Toolbar button (optional)

**Note:** All pop-out windows in Basic-10 should be non-modal.

---

## Project Structure

New files to create:

```
BASICtoMIPS_ByDogTired/
├── Basic10.Mcp/                      # New project: MCP Server
│   ├── Basic10.Mcp.csproj           # Console app targeting net8.0
│   ├── Program.cs                    # Entry point, stdio handling
│   ├── McpServer.cs                  # JSON-RPC message handling
│   ├── Tools/
│   │   ├── CodeTools.cs             # get_code, set_code
│   │   ├── CompileTools.cs          # compile, get_errors
│   │   ├── SymbolTools.cs           # get_symbols
│   │   └── DeviceTools.cs           # lookup_device, get_properties
│   └── HttpBridge.cs                # HTTP client to talk to Basic-10
│
├── UI/
│   ├── ClaudeAssistantWindow.xaml   # Chat window UI
│   ├── ClaudeAssistantWindow.xaml.cs
│   └── MainWindow.xaml.cs           # Add menu item, keyboard shortcut
│
├── Services/
│   ├── HttpApiServer.cs             # ASP.NET Core minimal API server
│   ├── ChatBridgeService.cs         # Routes messages between UI ↔ MCP
│   └── EditorBridgeService.cs       # Exposes editor operations for API
│
└── BasicToMips.csproj               # Add Microsoft.AspNetCore.App reference
```

**NuGet packages needed:**
- `Microsoft.AspNetCore.App` (for HTTP server in Basic-10)
- `System.Text.Json` (already have)
- None for MCP server (pure .NET, no dependencies)

---

## Configuration

### Claude Code Configuration

File: `~/.claude.json` or project `.claude/settings.json`

```json
{
  "mcpServers": {
    "basic10": {
      "command": "C:/path/to/Basic10.Mcp.exe",
      "args": [],
      "env": {
        "BASIC10_API_PORT": "19410"
      }
    }
  }
}
```

### Basic-10 Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `ApiServerEnabled` | `true` | Enable/disable HTTP API server |
| `ApiServerPort` | `19410` | Port for HTTP API |
| `ChatWindowEnabled` | `true` | Show Claude Assistant menu item |

---

## Startup Sequence

1. Basic-10 launches → starts HTTP server on port 19410
2. User runs Claude Code in this project folder
3. Claude Code spawns `Basic10.Mcp.exe`
4. MCP server connects to Basic-10's HTTP API
5. User opens Claude Chat window in Basic-10
6. Messages flow: Chat Window ↔ HTTP API ↔ MCP ↔ Claude Code ↔ Claude

---

## Status Indicators

| State | Chat Window Shows |
|-------|-------------------|
| Basic-10 API running, no MCP | "● API Ready - Waiting for Claude Code" |
| MCP connected | "● Connected to Claude Code" |
| MCP disconnected | "○ Disconnected - Start Claude Code with MCP" |

---

## Future Enhancements (Not in v1)

- F: File operations (Open/Save via API)
- G: Simulator control (Start/Stop/Step)
- Direct Anthropic API support (when Claude Code not running)
- Chat history persistence

---

## Implementation Order

1. HTTP API Server in Basic-10
2. EditorBridgeService (expose editor operations)
3. MCP Server project (Basic10.Mcp.exe)
4. MCP tools implementation
5. Chat Window UI
6. ChatBridgeService (message routing)
7. Configuration and settings
8. Testing and documentation
