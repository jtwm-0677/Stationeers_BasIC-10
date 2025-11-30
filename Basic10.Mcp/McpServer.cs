using System.Text.Json;
using System.Text.Json.Nodes;

namespace Basic10.Mcp;

/// <summary>
/// MCP Server that handles JSON-RPC 2.0 communication over stdio.
/// Implements the Model Context Protocol for Claude Code integration.
/// </summary>
public class McpServer
{
    private readonly HttpBridge _bridge;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServer(HttpBridge bridge)
    {
        _bridge = bridge;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Main loop - reads JSON-RPC messages from stdin, processes them, writes responses to stdout.
    /// </summary>
    public async Task Run()
    {
        using var reader = Console.In;

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break; // EOF

            try
            {
                var request = JsonNode.Parse(line);
                if (request == null) continue;

                var response = await ProcessRequest(request);
                if (response != null)
                {
                    var responseJson = response.ToJsonString(_jsonOptions);
                    Console.WriteLine(responseJson);
                    Console.Out.Flush();
                }
            }
            catch (Exception ex)
            {
                WriteError(-1, -32603, $"Internal error: {ex.Message}");
            }
        }
    }

    private async Task<JsonNode?> ProcessRequest(JsonNode request)
    {
        var method = request["method"]?.GetValue<string>();
        var id = request["id"];
        var paramsNode = request["params"];

        if (method == null) return null;

        try
        {
            object? result = method switch
            {
                "initialize" => HandleInitialize(paramsNode),
                "notifications/initialized" => null, // No response needed
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolsCall(paramsNode),
                _ => throw new Exception($"Unknown method: {method}")
            };

            if (id == null) return null; // Notification, no response

            return CreateResponse(id, result);
        }
        catch (Exception ex)
        {
            if (id != null)
            {
                return CreateErrorResponse(id, -32603, ex.Message);
            }
            return null;
        }
    }

    private object HandleInitialize(JsonNode? paramsNode)
    {
        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { listChanged = false }
            },
            serverInfo = new
            {
                name = "basic10",
                version = "1.0.0"
            }
        };
    }

    private object HandleToolsList()
    {
        return new
        {
            tools = new object[]
            {
                new
                {
                    name = "basic10_get_code",
                    description = "Get the current BASIC source code from the Basic-10 editor",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_set_code",
                    description = "Replace the editor content with new BASIC code. The editor will auto-compile if enabled.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            code = new
                            {
                                type = "string",
                                description = "The BASIC source code to set in the editor"
                            }
                        },
                        required = new[] { "code" }
                    }
                },
                new
                {
                    name = "basic10_compile",
                    description = "Compile the current BASIC code and return the IC10 output with any errors or warnings",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_get_symbols",
                    description = "Get all defined symbols from the current code: variables, labels, aliases, constants, and functions",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_lookup_device",
                    description = "Search the device database by name or get a device by its hash",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            query = new
                            {
                                type = "string",
                                description = "Device name to search for"
                            },
                            hash = new
                            {
                                type = "integer",
                                description = "Device hash to look up directly"
                            }
                        },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_get_properties",
                    description = "Get the list of all available device logic properties (Temperature, Pressure, On, etc.)",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            query = new
                            {
                                type = "string",
                                description = "Optional filter to search properties by name"
                            }
                        },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_get_messages",
                    description = "Get pending messages from the user via the Claude Assistant window in Basic-10. Call this to check for user requests.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_send_response",
                    description = "Send a response back to the user via the Claude Assistant window in Basic-10",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            content = new
                            {
                                type = "string",
                                description = "The response message to send to the user"
                            },
                            messageId = new
                            {
                                type = "string",
                                description = "Optional ID of the message being replied to"
                            }
                        },
                        required = new[] { "content" }
                    }
                }
            }
        };
    }

    private async Task<object> HandleToolsCall(JsonNode? paramsNode)
    {
        var toolName = paramsNode?["name"]?.GetValue<string>();
        var arguments = paramsNode?["arguments"];

        if (toolName == null)
            throw new Exception("Missing tool name");

        return toolName switch
        {
            "basic10_get_code" => await CallGetCode(),
            "basic10_set_code" => await CallSetCode(arguments),
            "basic10_compile" => await CallCompile(),
            "basic10_get_symbols" => await CallGetSymbols(),
            "basic10_lookup_device" => await CallLookupDevice(arguments),
            "basic10_get_properties" => await CallGetProperties(arguments),
            "basic10_get_messages" => await CallGetMessages(),
            "basic10_send_response" => await CallSendResponse(arguments),
            _ => throw new Exception($"Unknown tool: {toolName}")
        };
    }

    private async Task<object> CallGetCode()
    {
        var result = await _bridge.GetCode();
        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = result
                }
            }
        };
    }

    private async Task<object> CallSetCode(JsonNode? arguments)
    {
        var code = arguments?["code"]?.GetValue<string>();
        if (code == null)
            throw new Exception("Missing 'code' argument");

        await _bridge.SetCode(code);
        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = "Code updated successfully in Basic-10 editor"
                }
            }
        };
    }

    private async Task<object> CallCompile()
    {
        var result = await _bridge.Compile();
        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = result
                }
            }
        };
    }

    private async Task<object> CallGetSymbols()
    {
        var result = await _bridge.GetSymbols();
        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = result
                }
            }
        };
    }

    private async Task<object> CallLookupDevice(JsonNode? arguments)
    {
        var query = arguments?["query"]?.GetValue<string>();
        var hash = arguments?["hash"]?.GetValue<int>();

        var result = await _bridge.LookupDevice(query, hash);
        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = result
                }
            }
        };
    }

    private async Task<object> CallGetProperties(JsonNode? arguments)
    {
        var query = arguments?["query"]?.GetValue<string>();
        var result = await _bridge.GetProperties(query);
        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = result
                }
            }
        };
    }

    private async Task<object> CallGetMessages()
    {
        var result = await _bridge.GetMessages();
        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = result
                }
            }
        };
    }

    private async Task<object> CallSendResponse(JsonNode? arguments)
    {
        var content = arguments?["content"]?.GetValue<string>();
        var messageId = arguments?["messageId"]?.GetValue<string>();

        if (content == null)
            throw new Exception("Missing 'content' argument");

        var result = await _bridge.SendResponse(content, messageId);
        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = result
                }
            }
        };
    }

    private JsonNode CreateResponse(JsonNode? id, object? result)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone()
        };

        if (result != null)
        {
            response["result"] = JsonSerializer.SerializeToNode(result, _jsonOptions);
        }

        return response;
    }

    private JsonNode CreateErrorResponse(JsonNode? id, int code, string message)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
    }

    private void WriteError(int id, int code, string message)
    {
        var error = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
        Console.WriteLine(error.ToJsonString());
        Console.Out.Flush();
    }
}
