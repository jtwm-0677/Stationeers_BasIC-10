using System.Text.Json;
using BasicToMips.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BasicToMips.Services;

/// <summary>
/// HTTP API Server for MCP integration.
/// Exposes editor operations via REST endpoints on localhost.
/// </summary>
public class HttpApiServer
{
    private WebApplication? _app;
    private readonly EditorBridgeService _bridge;
    private readonly int _port;
    private Task? _runTask;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _app != null && _runTask != null && !_runTask.IsCompleted;
    public int Port => _port;

    public HttpApiServer(EditorBridgeService bridge, int port = 19410)
    {
        _bridge = bridge;
        _port = port;
    }

    /// <summary>
    /// Start the HTTP API server.
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;

        try
        {
            _cts = new CancellationTokenSource();

            var builder = WebApplication.CreateBuilder();

            // Configure minimal logging to avoid console spam
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            // Configure Kestrel to listen on localhost only
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(_port);
            });

            // Add CORS for local development
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            _app = builder.Build();
            _app.UseCors();

            // Register endpoints
            RegisterEndpoints(_app);

            // Run in background
            _runTask = Task.Run(() => _app.RunAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start HTTP API server: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop the HTTP API server.
    /// </summary>
    public void Stop()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        if (_app != null)
        {
            _app.StopAsync().Wait(TimeSpan.FromSeconds(5));
            _app.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
            _app = null;
        }

        _runTask = null;
    }

    private void RegisterEndpoints(WebApplication app)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Health check / status endpoint
        app.MapGet("/api/status", () => Results.Ok(new
        {
            status = "running",
            version = "1.0.0",
            app = "Basic-10"
        }));

        // === Code Operations ===

        // GET /api/code - Get current BASIC source
        app.MapGet("/api/code", () =>
        {
            try
            {
                var code = _bridge.GetCode();
                return Results.Ok(new { code });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/code - Set BASIC source (replaces editor content)
        app.MapPost("/api/code", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<CodeRequest>(ctx.Request.Body, jsonOptions);
                if (body?.Code == null)
                    return Results.BadRequest(new { error = "Missing 'code' field" });

                _bridge.SetCode(body.Code);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // PATCH /api/code - Insert code at cursor or line
        app.MapPatch("/api/code", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<InsertCodeRequest>(ctx.Request.Body, jsonOptions);
                if (body?.Code == null)
                    return Results.BadRequest(new { error = "Missing 'code' field" });

                _bridge.InsertCode(body.Code, body.AtLine);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // === Compilation ===

        // POST /api/compile - Compile current code
        app.MapPost("/api/compile", () =>
        {
            try
            {
                var result = _bridge.Compile();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/ic10 - Get current IC10 output
        app.MapGet("/api/ic10", () =>
        {
            try
            {
                var ic10 = _bridge.GetIc10Output();
                return Results.Ok(new { ic10 });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/errors - Get compilation errors
        app.MapGet("/api/errors", () =>
        {
            try
            {
                var errors = _bridge.GetErrors();
                return Results.Ok(new { errors });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // === Symbols ===

        // GET /api/symbols - Get all symbols
        app.MapGet("/api/symbols", () =>
        {
            try
            {
                var symbols = _bridge.GetSymbols();
                return Results.Ok(symbols);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // === Device Database ===

        // GET /api/devices - Search/list devices
        app.MapGet("/api/devices", (string? query, int? hash) =>
        {
            try
            {
                List<DeviceApiResponse> devices;

                if (hash.HasValue)
                {
                    // Find device by hash
                    var device = DeviceDatabase.Devices.FirstOrDefault(d => d.Hash == hash.Value);
                    devices = device != null
                        ? new List<DeviceApiResponse> { ToDeviceResponse(device) }
                        : new List<DeviceApiResponse>();
                }
                else if (!string.IsNullOrEmpty(query))
                {
                    // Search by query
                    devices = DeviceDatabase.SearchDevices(query)
                        .Select(ToDeviceResponse)
                        .ToList();
                }
                else
                {
                    // Return all devices
                    devices = DeviceDatabase.Devices
                        .Select(ToDeviceResponse)
                        .ToList();
                }

                return Results.Ok(new { devices, count = devices.Count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/devices/{hash} - Get device by hash
        app.MapGet("/api/devices/{hash:int}", (int hash) =>
        {
            try
            {
                var device = DeviceDatabase.Devices.FirstOrDefault(d => d.Hash == hash);
                if (device == null)
                    return Results.NotFound(new { error = "Device not found" });

                return Results.Ok(ToDeviceResponse(device));
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/properties - Get all logic properties
        app.MapGet("/api/properties", (string? query) =>
        {
            try
            {
                var properties = string.IsNullOrEmpty(query)
                    ? DeviceDatabase.LogicTypes
                    : DeviceDatabase.SearchLogicTypes(query);

                var response = properties.Select(p => new PropertyApiResponse
                {
                    Name = p.Name,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    Hash = p.Hash,
                    Value = p.Value
                }).ToList();

                return Results.Ok(new { properties = response, count = response.Count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/slot-properties - Get all slot logic properties
        app.MapGet("/api/slot-properties", () =>
        {
            try
            {
                var properties = DeviceDatabase.SlotLogicTypes.Select(p => new PropertyApiResponse
                {
                    Name = p.Name,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    Hash = p.Hash,
                    Value = p.Value
                }).ToList();

                return Results.Ok(new { properties, count = properties.Count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // === Cursor/Editor State ===

        // GET /api/cursor - Get cursor position
        app.MapGet("/api/cursor", () =>
        {
            try
            {
                var position = _bridge.GetCursorPosition();
                return Results.Ok(position);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/cursor - Set cursor position
        app.MapPost("/api/cursor", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<CursorRequest>(ctx.Request.Body, jsonOptions);
                if (body == null)
                    return Results.BadRequest(new { error = "Invalid request body" });

                _bridge.SetCursorPosition(body.Line, body.Column);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // === Message Queue (AI Assistant) ===

        // POST /api/messages - User sends a message
        app.MapPost("/api/messages", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<ChatMessageRequest>(ctx.Request.Body, jsonOptions);
                if (string.IsNullOrWhiteSpace(body?.Content))
                    return Results.BadRequest(new { error = "Missing 'content' field" });

                var id = MessageQueue.EnqueueUserMessage(body.Content);
                return Results.Ok(new { success = true, messageId = id });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/messages - AI polls for pending messages
        app.MapGet("/api/messages", () =>
        {
            try
            {
                var messages = MessageQueue.GetPendingUserMessages();
                return Results.Ok(new
                {
                    messages = messages.Select(m => new
                    {
                        id = m.Id,
                        content = m.Content,
                        timestamp = m.Timestamp.ToString("o")
                    }),
                    count = messages.Count
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/messages/response - AI sends a response
        app.MapPost("/api/messages/response", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<ChatResponseRequest>(ctx.Request.Body, jsonOptions);
                if (string.IsNullOrWhiteSpace(body?.Content))
                    return Results.BadRequest(new { error = "Missing 'content' field" });

                MessageQueue.EnqueueAIResponse(body.Content, body.MessageId);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/messages/responses - UI polls for AI responses
        app.MapGet("/api/messages/responses", () =>
        {
            try
            {
                var messages = MessageQueue.GetPendingAIResponses();
                return Results.Ok(new
                {
                    messages = messages.Select(m => new
                    {
                        id = m.Id,
                        content = m.Content,
                        timestamp = m.Timestamp.ToString("o"),
                        replyToId = m.ReplyToId
                    }),
                    count = messages.Count
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // === Tab Management ===

        // POST /api/tabs - Create a new tab
        app.MapPost("/api/tabs", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<NewTabRequest>(ctx.Request.Body, jsonOptions);
                var tabIndex = _bridge.CreateNewTab(body?.Name);
                return Results.Ok(new { success = true, tabIndex });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/tabs - List all open tabs
        app.MapGet("/api/tabs", () =>
        {
            try
            {
                var tabs = _bridge.GetTabs();
                return Results.Ok(new { tabs, count = tabs.Count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/tabs/switch - Switch to a tab
        app.MapPost("/api/tabs/switch", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<SwitchTabRequest>(ctx.Request.Body, jsonOptions);
                if (body == null)
                    return Results.BadRequest(new { error = "Invalid request body" });

                bool success;
                if (body.TabIndex.HasValue)
                {
                    success = _bridge.SwitchTab(body.TabIndex.Value);
                }
                else if (!string.IsNullOrEmpty(body.TabName))
                {
                    success = _bridge.SwitchTabByName(body.TabName);
                }
                else
                {
                    return Results.BadRequest(new { error = "Must provide tabIndex or tabName" });
                }

                return Results.Ok(new { success });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // DELETE /api/tabs/{index} - Close a tab
        // Query param: force=true to skip save prompt
        app.MapDelete("/api/tabs/{index:int}", (int index, bool? force) =>
        {
            try
            {
                var success = _bridge.CloseTab(index, force ?? false);
                return Results.Ok(new { success });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // === Script Save/Load ===

        // GET /api/scripts - List all scripts in scripts folder
        app.MapGet("/api/scripts", () =>
        {
            try
            {
                var scripts = _bridge.ListScripts();
                return Results.Ok(new { scripts, count = scripts.Count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/scripts/save - Save current script to folder
        app.MapPost("/api/scripts/save", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<SaveScriptRequest>(ctx.Request.Body, jsonOptions);
                if (string.IsNullOrWhiteSpace(body?.ScriptName))
                    return Results.BadRequest(new { error = "Missing 'scriptName' field" });

                var result = _bridge.SaveScript(body.ScriptName);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/scripts/load - Load a script from folder
        app.MapPost("/api/scripts/load", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<LoadScriptRequest>(ctx.Request.Body, jsonOptions);
                if (string.IsNullOrWhiteSpace(body?.ScriptName))
                    return Results.BadRequest(new { error = "Missing 'scriptName' field" });

                var result = _bridge.LoadScript(body.ScriptName, body.NewTab ?? false);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ==================== SIMULATOR ENDPOINTS ====================

        // POST /api/simulator/start - Initialize simulator with IC10 code
        app.MapPost("/api/simulator/start", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<SimulatorStartRequest>(ctx.Request.Body, jsonOptions);
                var result = _bridge.SimulatorStart(body?.Ic10Code);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/simulator/stop - Stop the simulator
        app.MapPost("/api/simulator/stop", () =>
        {
            try
            {
                var result = _bridge.SimulatorStop();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/simulator/reset - Reset the simulator
        app.MapPost("/api/simulator/reset", () =>
        {
            try
            {
                var result = _bridge.SimulatorReset();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/simulator/step - Step one instruction
        app.MapPost("/api/simulator/step", () =>
        {
            try
            {
                var result = _bridge.SimulatorStep();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/simulator/run - Run until breakpoint or halt
        app.MapPost("/api/simulator/run", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<SimulatorRunRequest>(ctx.Request.Body, jsonOptions);
                var result = _bridge.SimulatorRun(body?.MaxInstructions ?? 10000);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/simulator/state - Get current simulator state
        app.MapGet("/api/simulator/state", () =>
        {
            try
            {
                var result = _bridge.SimulatorGetState();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/simulator/register - Set a register value
        app.MapPost("/api/simulator/register", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<SimulatorSetRegisterRequest>(ctx.Request.Body, jsonOptions);
                if (string.IsNullOrWhiteSpace(body?.Register))
                    return Results.BadRequest(new { error = "Missing 'register' field" });

                var result = _bridge.SimulatorSetRegister(body.Register, body.Value);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/simulator/breakpoint - Add a breakpoint
        app.MapPost("/api/simulator/breakpoint", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<SimulatorBreakpointRequest>(ctx.Request.Body, jsonOptions);
                var result = _bridge.SimulatorAddBreakpoint(body?.Line ?? 0);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // DELETE /api/simulator/breakpoint/{line} - Remove a breakpoint
        app.MapDelete("/api/simulator/breakpoint/{line:int}", (int line) =>
        {
            try
            {
                var result = _bridge.SimulatorRemoveBreakpoint(line);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // DELETE /api/simulator/breakpoints - Clear all breakpoints
        app.MapDelete("/api/simulator/breakpoints", () =>
        {
            try
            {
                var result = _bridge.SimulatorClearBreakpoints();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/simulator/device - Set a device property
        app.MapPost("/api/simulator/device", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<SimulatorDeviceRequest>(ctx.Request.Body, jsonOptions);
                if (string.IsNullOrWhiteSpace(body?.Property))
                    return Results.BadRequest(new { error = "Missing 'property' field" });

                var result = _bridge.SimulatorSetDeviceProperty(body.DeviceIndex, body.Property, body.Value);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/simulator/device/{index}/{property} - Get a device property
        app.MapGet("/api/simulator/device/{index:int}/{property}", (int index, string property) =>
        {
            try
            {
                var value = _bridge.SimulatorGetDeviceProperty(index, property);
                return Results.Ok(new { deviceIndex = index, property, value });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/simulator/device/slot - Set a device slot property
        app.MapPost("/api/simulator/device/slot", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<SimulatorDeviceSlotRequest>(ctx.Request.Body, jsonOptions);
                if (string.IsNullOrWhiteSpace(body?.Property))
                    return Results.BadRequest(new { error = "Missing 'property' field" });

                var result = _bridge.SimulatorSetDeviceSlotProperty(body.DeviceIndex, body.Slot, body.Property, body.Value);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ==================== DEBUGGING ENDPOINTS ====================

        // POST /api/debug/watch - Add a watch expression
        app.MapPost("/api/debug/watch", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<WatchRequest>(ctx.Request.Body, jsonOptions);
                if (string.IsNullOrWhiteSpace(body?.Expression))
                    return Results.BadRequest(new { error = "Missing 'expression' field" });

                var result = _bridge.AddWatch(body.Expression);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // DELETE /api/debug/watch/{name} - Remove a watch
        app.MapDelete("/api/debug/watch/{name}", (string name) =>
        {
            try
            {
                var result = _bridge.RemoveWatch(Uri.UnescapeDataString(name));
                return Results.Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/debug/watches - Get all watches
        app.MapGet("/api/debug/watches", () =>
        {
            try
            {
                var result = _bridge.GetWatches();
                return Results.Ok(new { watches = result, count = result.Count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // DELETE /api/debug/watches - Clear all watches
        app.MapDelete("/api/debug/watches", () =>
        {
            try
            {
                _bridge.ClearWatches();
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/debug/breakpoint - Add a BASIC breakpoint
        app.MapPost("/api/debug/breakpoint", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<BreakpointRequest>(ctx.Request.Body, jsonOptions);
                var result = _bridge.AddEditorBreakpoint(body?.Line ?? 1);
                return Results.Ok(new { breakpoints = result });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // DELETE /api/debug/breakpoint/{line} - Remove a BASIC breakpoint
        app.MapDelete("/api/debug/breakpoint/{line:int}", (int line) =>
        {
            try
            {
                var result = _bridge.RemoveEditorBreakpoint(line);
                return Results.Ok(new { breakpoints = result });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/debug/breakpoint/toggle - Toggle a BASIC breakpoint
        app.MapPost("/api/debug/breakpoint/toggle", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<BreakpointRequest>(ctx.Request.Body, jsonOptions);
                var result = _bridge.ToggleEditorBreakpoint(body?.Line ?? 1);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/debug/breakpoints - Get all BASIC breakpoints
        app.MapGet("/api/debug/breakpoints", () =>
        {
            try
            {
                var result = _bridge.GetEditorBreakpoints();
                return Results.Ok(new { breakpoints = result, count = result.Count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // DELETE /api/debug/breakpoints - Clear all BASIC breakpoints
        app.MapDelete("/api/debug/breakpoints", () =>
        {
            try
            {
                _bridge.ClearEditorBreakpoints();
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/debug/sourcemap - Get the source map
        app.MapGet("/api/debug/sourcemap", () =>
        {
            try
            {
                var result = _bridge.GetSourceMap();
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/debug/errors - Get compilation errors
        app.MapGet("/api/debug/errors", () =>
        {
            try
            {
                var result = _bridge.GetCompilationErrors();
                return Results.Ok(new { errors = result, count = result.Count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // POST /api/debug/goto - Navigate to a line
        app.MapPost("/api/debug/goto", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<GoToLineRequest>(ctx.Request.Body, jsonOptions);
                _bridge.GoToLine(body?.Line ?? 1);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ==================== SETTINGS ENDPOINTS ====================

        // GET /api/settings - Get all settings
        app.MapGet("/api/settings", () =>
        {
            try
            {
                return Results.Ok(_bridge.GetSettings());
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // PUT /api/settings - Update a setting
        app.MapPut("/api/settings", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<UpdateSettingRequest>(ctx.Request.Body, jsonOptions);
                if (body == null || string.IsNullOrEmpty(body.Name))
                    return Results.BadRequest(new { error = "Missing setting name" });

                var result = _bridge.UpdateSetting(body.Name, body.Value ?? "");
                if (result.Success)
                    return Results.Ok(result);
                else
                    return Results.BadRequest(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // ==================== CODE ANALYSIS ENDPOINTS ====================

        // POST /api/analysis/references - Find all references to a symbol
        app.MapPost("/api/analysis/references", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<FindReferencesRequest>(ctx.Request.Body, jsonOptions);
                if (body == null || string.IsNullOrEmpty(body.SymbolName))
                    return Results.BadRequest(new { error = "Missing symbolName" });

                var refs = _bridge.FindReferences(body.SymbolName);
                return Results.Ok(new { references = refs, count = refs.Count });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/analysis/metrics - Get code metrics
        app.MapGet("/api/analysis/metrics", () =>
        {
            try
            {
                return Results.Ok(_bridge.GetCodeMetrics());
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }

    private static DeviceApiResponse ToDeviceResponse(DeviceInfo device) => new()
    {
        PrefabName = device.PrefabName,
        DisplayName = device.DisplayName,
        Category = device.Category,
        Description = device.Description,
        Hash = device.Hash
    };
}

#region Request/Response Models

internal class CodeRequest
{
    public string? Code { get; set; }
}

internal class InsertCodeRequest
{
    public string? Code { get; set; }
    public int? AtLine { get; set; }
}

internal class CursorRequest
{
    public int Line { get; set; }
    public int Column { get; set; }
}

internal class DeviceApiResponse
{
    public string PrefabName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public int Hash { get; set; }
}

internal class PropertyApiResponse
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int Hash { get; set; }
    public int Value { get; set; }
}

internal class ChatMessageRequest
{
    public string? Content { get; set; }
}

internal class ChatResponseRequest
{
    public string? Content { get; set; }
    public string? MessageId { get; set; }
}

internal class NewTabRequest
{
    public string? Name { get; set; }
}

internal class SwitchTabRequest
{
    public int? TabIndex { get; set; }
    public string? TabName { get; set; }
}

internal class SaveScriptRequest
{
    public string? ScriptName { get; set; }
}

internal class LoadScriptRequest
{
    public string? ScriptName { get; set; }
    public bool? NewTab { get; set; }
}

internal class SimulatorStartRequest
{
    public string? Ic10Code { get; set; }
}

internal class SimulatorRunRequest
{
    public int? MaxInstructions { get; set; }
}

internal class SimulatorSetRegisterRequest
{
    public string? Register { get; set; }
    public double Value { get; set; }
}

internal class SimulatorBreakpointRequest
{
    public int Line { get; set; }
}

internal class SimulatorDeviceRequest
{
    public int DeviceIndex { get; set; }
    public string? Property { get; set; }
    public double Value { get; set; }
}

internal class SimulatorDeviceSlotRequest
{
    public int DeviceIndex { get; set; }
    public int Slot { get; set; }
    public string? Property { get; set; }
    public double Value { get; set; }
}

internal class WatchRequest
{
    public string? Expression { get; set; }
}

internal class BreakpointRequest
{
    public int Line { get; set; }
}

internal class GoToLineRequest
{
    public int Line { get; set; }
}

internal class UpdateSettingRequest
{
    public string? Name { get; set; }
    public object? Value { get; set; }
}

internal class FindReferencesRequest
{
    public string? SymbolName { get; set; }
}

#endregion

#region Message Queue

/// <summary>
/// Thread-safe message queue for AI Assistant chat.
/// </summary>
public static class MessageQueue
{
    private static readonly object _lock = new();
    private static readonly List<QueuedMessage> _outgoing = new();
    private static readonly List<QueuedMessage> _incoming = new();

    /// <summary>
    /// Add a message from the user to be picked up by the AI.
    /// </summary>
    public static string EnqueueUserMessage(string content)
    {
        lock (_lock)
        {
            var msg = new QueuedMessage
            {
                Id = Guid.NewGuid().ToString("N"),
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsFromUser = true
            };
            _outgoing.Add(msg);
            return msg.Id;
        }
    }

    /// <summary>
    /// Add a response from the AI to be displayed in the UI.
    /// </summary>
    public static void EnqueueAIResponse(string content, string? replyToId = null)
    {
        lock (_lock)
        {
            var msg = new QueuedMessage
            {
                Id = Guid.NewGuid().ToString("N"),
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsFromUser = false,
                ReplyToId = replyToId
            };
            _incoming.Add(msg);
        }
    }

    /// <summary>
    /// Get all pending user messages (for AI to pick up).
    /// </summary>
    public static List<QueuedMessage> GetPendingUserMessages()
    {
        lock (_lock)
        {
            var messages = _outgoing.ToList();
            _outgoing.Clear();
            return messages;
        }
    }

    /// <summary>
    /// Get all pending AI responses (for UI to display).
    /// </summary>
    public static List<QueuedMessage> GetPendingAIResponses()
    {
        lock (_lock)
        {
            var messages = _incoming.ToList();
            _incoming.Clear();
            return messages;
        }
    }

    /// <summary>
    /// Check if there are pending user messages without consuming them.
    /// </summary>
    public static bool HasPendingUserMessages()
    {
        lock (_lock)
        {
            return _outgoing.Count > 0;
        }
    }
}

public class QueuedMessage
{
    public string Id { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool IsFromUser { get; set; }
    public string? ReplyToId { get; set; }
}

#endregion
