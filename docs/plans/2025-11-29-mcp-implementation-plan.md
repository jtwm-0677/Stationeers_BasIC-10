# MCP Integration Implementation Plan

**Date:** 2025-11-29
**Design Doc:** `docs/plans/2025-11-29-mcp-integration-design.md`
**Status:** Ready for implementation

---

## Phase 1: HTTP API Server in Basic-10

### Task 1.1: Add ASP.NET Core Dependencies

**File:** `BasicToMips.csproj`

Add to ItemGroup:
```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

**Verification:** `dotnet build -c Release` succeeds

---

### Task 1.2: Create EditorBridgeService

**File:** `Services/EditorBridgeService.cs`

This service exposes editor operations to the HTTP API. Needs access to MainWindow.

```csharp
namespace BasicToMips.Services;

public class EditorBridgeService
{
    private readonly MainWindow _mainWindow;

    // Methods needed:
    public string GetCode();                    // Get BASIC from editor
    public void SetCode(string code);           // Set BASIC in editor (dispatcher!)
    public (string ic10, List<Error> errors, int lineCount) Compile();
    public SymbolTable GetSymbols();            // Variables, labels, aliases, constants
    public string GetIc10Output();              // Current IC10 panel content
    public List<CompileError> GetErrors();      // Last compilation errors
}
```

**Key:** All UI operations must use `Dispatcher.Invoke()` since HTTP requests come from different thread.

**Verification:** Unit test or manual test calling methods from background thread.

---

### Task 1.3: Create HttpApiServer

**File:** `Services/HttpApiServer.cs`

Minimal API server using ASP.NET Core:

```csharp
namespace BasicToMips.Services;

public class HttpApiServer
{
    private WebApplication? _app;
    private readonly EditorBridgeService _bridge;
    private readonly int _port = 19410;

    public void Start();   // Start server on port
    public void Stop();    // Stop server

    // Endpoints to register:
    // GET  /api/code        → _bridge.GetCode()
    // POST /api/code        → _bridge.SetCode(body.code)
    // POST /api/compile     → _bridge.Compile()
    // GET  /api/ic10        → _bridge.GetIc10Output()
    // GET  /api/errors      → _bridge.GetErrors()
    // GET  /api/symbols     → _bridge.GetSymbols()
    // GET  /api/devices     → DeviceDatabase query
    // GET  /api/properties  → LogicTypes list
}
```

**Verification:**
```bash
curl http://localhost:19410/api/code
curl -X POST http://localhost:19410/api/code -H "Content-Type: application/json" -d "{\"code\":\"VAR x = 1\"}"
```

---

### Task 1.4: Integrate HTTP Server into MainWindow

**File:** `UI/MainWindow.xaml.cs`

- Create `EditorBridgeService` instance in constructor
- Create `HttpApiServer` instance, passing bridge
- Start server in `Window_Loaded`
- Stop server in `Window_Closing`
- Add settings for port and enabled state

**Verification:** Launch app, server starts, curl commands work.

---

### Task 1.5: Add API Settings to SettingsService

**File:** `UI/Services/SettingsService.cs`

Add properties:
```csharp
public bool ApiServerEnabled { get; set; } = true;
public int ApiServerPort { get; set; } = 19410;
```

**File:** `UI/MainWindow.xaml.cs` - Check settings before starting server

**Verification:** Disable in settings, restart app, server doesn't start.

---

## Phase 2: MCP Server Project

### Task 2.1: Create MCP Project Structure

**Commands:**
```bash
cd "C:\Development\Stationeers Stuff\BASICtoMIPS_ByDogTired"
mkdir Basic10.Mcp
cd Basic10.Mcp
dotnet new console -n Basic10.Mcp
```

**File:** `Basic10.Mcp/Basic10.Mcp.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Verification:** `dotnet build` in Basic10.Mcp folder succeeds.

---

### Task 2.2: Implement MCP Protocol Handler

**File:** `Basic10.Mcp/McpServer.cs`

MCP uses JSON-RPC 2.0 over stdio. Messages:
- `initialize` - Return server capabilities and tools
- `tools/list` - Return list of available tools
- `tools/call` - Execute a tool and return result

```csharp
public class McpServer
{
    public async Task Run();  // Main loop reading stdin, writing stdout

    private object HandleInitialize(JsonElement params);
    private object HandleToolsList(JsonElement params);
    private object HandleToolsCall(JsonElement params);
}
```

**Verification:** Run exe, send JSON-RPC initialize message, get valid response.

---

### Task 2.3: Implement HttpBridge

**File:** `Basic10.Mcp/HttpBridge.cs`

HTTP client to talk to Basic-10:

```csharp
public class HttpBridge
{
    private readonly HttpClient _client;
    private readonly string _baseUrl = "http://localhost:19410";

    public async Task<string> GetCode();
    public async Task SetCode(string code);
    public async Task<CompileResult> Compile();
    public async Task<SymbolTable> GetSymbols();
    public async Task<List<Device>> LookupDevice(string? query, int? hash);
    public async Task<List<Property>> GetProperties();
}
```

**Verification:** Run Basic-10, run MCP exe manually, verify HTTP calls work.

---

### Task 2.4: Implement MCP Tools

**File:** `Basic10.Mcp/Tools/CodeTools.cs`
```csharp
public static class CodeTools
{
    public static ToolDefinition GetCodeTool => new() { Name = "basic10_get_code", ... };
    public static ToolDefinition SetCodeTool => new() { Name = "basic10_set_code", ... };

    public static async Task<object> GetCode(HttpBridge bridge);
    public static async Task<object> SetCode(HttpBridge bridge, string code);
}
```

**File:** `Basic10.Mcp/Tools/CompileTools.cs`
- `basic10_compile` tool

**File:** `Basic10.Mcp/Tools/SymbolTools.cs`
- `basic10_get_symbols` tool

**File:** `Basic10.Mcp/Tools/DeviceTools.cs`
- `basic10_lookup_device` tool
- `basic10_get_properties` tool

**Verification:** Send tools/call via stdin, get correct results.

---

### Task 2.5: Update Solution File

Add Basic10.Mcp to solution:
```bash
dotnet sln add Basic10.Mcp/Basic10.Mcp.csproj
```

**Verification:** `dotnet build` from solution root builds both projects.

---

## Phase 3: Claude Chat Window

### Task 3.1: Create Chat Window XAML

**File:** `UI/ClaudeAssistantWindow.xaml`

Layout:
- Title bar: "Claude Assistant" with minimize/close buttons
- Status indicator (connected/disconnected)
- Scrollable message list
- Input textbox with Send button

**Key:** Set `ShowInTaskbar="True"` and do NOT set `Owner` to make it truly non-modal.

**Verification:** Window opens, can interact with main editor while open.

---

### Task 3.2: Create Chat Window Code-Behind

**File:** `UI/ClaudeAssistantWindow.xaml.cs`

```csharp
public partial class ClaudeAssistantWindow : Window
{
    public ObservableCollection<ChatMessage> Messages { get; }

    public void AddMessage(string sender, string text);
    public void SetConnectionStatus(ConnectionStatus status);

    private void SendButton_Click(object sender, RoutedEventArgs e);
    private void InputBox_KeyDown(object sender, KeyEventArgs e); // Enter to send
}
```

**Verification:** Can type messages, they appear in list.

---

### Task 3.3: Create ChatBridgeService

**File:** `Services/ChatBridgeService.cs`

Routes messages between Chat Window and MCP server:

```csharp
public class ChatBridgeService
{
    public event Action<string, string>? MessageReceived;
    public event Action<ConnectionStatus>? ConnectionChanged;

    public async Task SendMessage(string message);
    public void HandleMcpMessage(string message);  // Called when MCP sends notification
}
```

This connects to the HTTP API endpoints:
- `POST /api/chat/send` - Send user message
- `GET /api/chat/messages` - Poll for new messages (or WebSocket)

**Verification:** Send message from chat window, see it in MCP server logs.

---

### Task 3.4: Add Chat Bridge Endpoints to HTTP API

**File:** `Services/HttpApiServer.cs`

Add endpoints:
```csharp
app.MapPost("/api/chat/send", (ChatMessage msg) => ...);
app.MapGet("/api/chat/messages", () => ...);
// Optional: WebSocket at /api/chat/stream
```

**Verification:** curl POST to /api/chat/send works.

---

### Task 3.5: Integrate Chat Window into MainWindow

**File:** `UI/MainWindow.xaml`
- Add menu item: View → Claude Assistant (F10)
- Add keyboard binding for F10 and Ctrl+Shift+C

**File:** `UI/MainWindow.xaml.cs`
- Create ClaudeAssistantWindow instance
- Show/hide on menu click
- Keep reference to single instance

**Verification:** F10 opens chat window, can type and interact with editor.

---

## Phase 4: Configuration

### Task 4.1: Create Claude Code MCP Config

**File:** `.claude/mcp.json` (in project root)

```json
{
  "mcpServers": {
    "basic10": {
      "command": "dotnet",
      "args": ["run", "--project", "Basic10.Mcp"],
      "env": {
        "BASIC10_API_PORT": "19410"
      }
    }
  }
}
```

Or for published exe:
```json
{
  "mcpServers": {
    "basic10": {
      "command": "./Basic10.Mcp/bin/Release/net8.0/Basic10.Mcp.exe"
    }
  }
}
```

**Verification:** Start Claude Code in project folder, MCP server connects.

---

### Task 4.2: Add MCP Status to Chat Window

When chat window opens:
1. Check if MCP is connected (try HTTP call to special endpoint)
2. Show appropriate status message
3. Update status when connection changes

**Verification:** Open chat window with/without Claude Code running, see correct status.

---

## Phase 5: Testing & Polish

### Task 5.1: End-to-End Test

1. Start Basic-10
2. Start Claude Code with `claude` command
3. Verify MCP tools work: "Get the current code from Basic-10"
4. Open chat window in Basic-10
5. Send message from chat window
6. Verify Claude responds
7. Ask Claude to modify code
8. Verify editor updates live

---

### Task 5.2: Error Handling

- HTTP API returns proper error codes
- MCP server handles connection failures gracefully
- Chat window shows error messages nicely
- Timeout handling for slow responses

---

### Task 5.3: Documentation

Update `docs/UserGuide.md` with:
- MCP integration section
- Claude Assistant window usage
- Configuration instructions

---

## Implementation Order Summary

1. **Phase 1** (HTTP API) - Must complete first, foundation for everything
2. **Phase 2** (MCP Server) - Can test with curl before Phase 3
3. **Phase 3** (Chat Window) - UI integration
4. **Phase 4** (Configuration) - Connect everything
5. **Phase 5** (Testing) - Verify end-to-end

Estimated tasks: 15 discrete tasks across 5 phases.

---

## Quick Reference: Files to Create

| File | Purpose |
|------|---------|
| `Services/EditorBridgeService.cs` | Expose editor ops to API |
| `Services/HttpApiServer.cs` | ASP.NET Core minimal API |
| `Services/ChatBridgeService.cs` | Route chat messages |
| `Basic10.Mcp/Program.cs` | MCP server entry point |
| `Basic10.Mcp/McpServer.cs` | JSON-RPC handler |
| `Basic10.Mcp/HttpBridge.cs` | HTTP client to Basic-10 |
| `Basic10.Mcp/Tools/*.cs` | Tool implementations |
| `UI/ClaudeAssistantWindow.xaml` | Chat window UI |
| `UI/ClaudeAssistantWindow.xaml.cs` | Chat window logic |
| `.claude/mcp.json` | MCP configuration |

---

## Quick Reference: Files to Modify

| File | Changes |
|------|---------|
| `BasicToMips.csproj` | Add ASP.NET Core reference |
| `UI/MainWindow.xaml` | Add Claude Assistant menu item |
| `UI/MainWindow.xaml.cs` | Start HTTP server, manage chat window |
| `UI/Services/SettingsService.cs` | Add API settings |
