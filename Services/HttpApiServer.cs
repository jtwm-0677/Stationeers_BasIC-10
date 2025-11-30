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

        // === Message Queue (Claude Assistant) ===

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

        // GET /api/messages - Claude polls for pending messages
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

        // POST /api/messages/response - Claude sends a response
        app.MapPost("/api/messages/response", async (HttpContext ctx) =>
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<ChatResponseRequest>(ctx.Request.Body, jsonOptions);
                if (string.IsNullOrWhiteSpace(body?.Content))
                    return Results.BadRequest(new { error = "Missing 'content' field" });

                MessageQueue.EnqueueClaudeResponse(body.Content, body.MessageId);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });

        // GET /api/messages/responses - UI polls for Claude responses
        app.MapGet("/api/messages/responses", () =>
        {
            try
            {
                var messages = MessageQueue.GetPendingClaudeResponses();
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

#endregion

#region Message Queue

/// <summary>
/// Thread-safe message queue for Claude Assistant chat.
/// </summary>
public static class MessageQueue
{
    private static readonly object _lock = new();
    private static readonly List<QueuedMessage> _outgoing = new();
    private static readonly List<QueuedMessage> _incoming = new();

    /// <summary>
    /// Add a message from the user to be picked up by Claude.
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
    /// Add a response from Claude to be displayed in the UI.
    /// </summary>
    public static void EnqueueClaudeResponse(string content, string? replyToId = null)
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
    /// Get all pending user messages (for Claude to pick up).
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
    /// Get all pending Claude responses (for UI to display).
    /// </summary>
    public static List<QueuedMessage> GetPendingClaudeResponses()
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
