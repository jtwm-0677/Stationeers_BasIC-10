using System.Text.Json;
using System.Text.Json.Nodes;

namespace Basic10.Mcp;

/// <summary>
/// MCP Server that handles JSON-RPC 2.0 communication over stdio.
/// Implements the Model Context Protocol for AI assistant integration.
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
                    name = "basic10_format",
                    description = "Format the current BASIC code with proper indentation and spacing. Normalizes operator spacing and keyword casing.",
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
                    description = "Get pending messages from the user via the AI Assistant window in Basic-10. Call this to check for user requests.",
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
                    description = "Send a response back to the user via the AI Assistant window in Basic-10",
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
                },
                // Tab Management Tools
                new
                {
                    name = "basic10_new_tab",
                    description = "Create a new empty script tab in the Basic-10 editor",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new
                            {
                                type = "string",
                                description = "Optional name for the new tab"
                            }
                        },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_list_tabs",
                    description = "List all open tabs in the Basic-10 editor, showing which one is active",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_switch_tab",
                    description = "Switch to a different tab by index or name",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tabIndex = new
                            {
                                type = "integer",
                                description = "Tab index (0-based) to switch to"
                            },
                            tabName = new
                            {
                                type = "string",
                                description = "Tab name to switch to (alternative to tabIndex)"
                            }
                        },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_close_tab",
                    description = "Close a tab by its index",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tabIndex = new
                            {
                                type = "integer",
                                description = "Tab index (0-based) to close"
                            },
                            force = new
                            {
                                type = "boolean",
                                description = "If true, close without prompting to save unsaved changes (default: false)"
                            }
                        },
                        required = new[] { "tabIndex" }
                    }
                },
                // Script Save/Load Tools
                new
                {
                    name = "basic10_list_scripts",
                    description = "List all scripts in the Stationeers scripts folder",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_save_script",
                    description = "Save the current script to the Stationeers scripts folder. Creates a folder with the script name containing .bas file and instruction.xml",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            scriptName = new
                            {
                                type = "string",
                                description = "Name for the script folder (will be the script name in-game)"
                            }
                        },
                        required = new[] { "scriptName" }
                    }
                },
                new
                {
                    name = "basic10_load_script",
                    description = "Load a script from the Stationeers scripts folder by its folder name",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            scriptName = new
                            {
                                type = "string",
                                description = "Name of the script folder to load"
                            },
                            newTab = new
                            {
                                type = "boolean",
                                description = "If true, load into a new tab; otherwise load into current tab"
                            }
                        },
                        required = new[] { "scriptName" }
                    }
                },
                // Simulator Tools
                new
                {
                    name = "basic10_simulator_start",
                    description = "Initialize the IC10 simulator with compiled code. Uses current compiled output if no code provided.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            ic10Code = new
                            {
                                type = "string",
                                description = "Optional IC10 code to simulate. If not provided, uses the current compiled output."
                            }
                        },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_simulator_stop",
                    description = "Stop and halt the IC10 simulator",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_simulator_reset",
                    description = "Reset the IC10 simulator to initial state",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_simulator_step",
                    description = "Execute a single IC10 instruction and return the new state",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_simulator_run",
                    description = "Run the IC10 simulator until it hits a breakpoint, halt, or yield instruction",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            maxInstructions = new
                            {
                                type = "integer",
                                description = "Maximum instructions to execute before stopping (default: 10000)"
                            }
                        },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_simulator_get_state",
                    description = "Get the current IC10 simulator state: registers, stack, devices, breakpoints, etc.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_simulator_set_register",
                    description = "Set a register value in the IC10 simulator",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            register = new
                            {
                                type = "string",
                                description = "Register name (r0-r15 or ra)"
                            },
                            value = new
                            {
                                type = "number",
                                description = "Value to set"
                            }
                        },
                        required = new[] { "register", "value" }
                    }
                },
                new
                {
                    name = "basic10_simulator_add_breakpoint",
                    description = "Add a breakpoint at a specific IC10 line number",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            line = new
                            {
                                type = "integer",
                                description = "Line number (0-based) to set breakpoint"
                            }
                        },
                        required = new[] { "line" }
                    }
                },
                new
                {
                    name = "basic10_simulator_remove_breakpoint",
                    description = "Remove a breakpoint from a specific IC10 line number",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            line = new
                            {
                                type = "integer",
                                description = "Line number (0-based) to remove breakpoint from"
                            }
                        },
                        required = new[] { "line" }
                    }
                },
                new
                {
                    name = "basic10_simulator_clear_breakpoints",
                    description = "Clear all breakpoints from the IC10 simulator",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_simulator_set_device",
                    description = "Set a property on a simulated device (d0-d5)",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            deviceIndex = new
                            {
                                type = "integer",
                                description = "Device index (0-5)"
                            },
                            property = new
                            {
                                type = "string",
                                description = "Property name (On, Setting, Temperature, Pressure, etc.)"
                            },
                            value = new
                            {
                                type = "number",
                                description = "Value to set"
                            }
                        },
                        required = new[] { "deviceIndex", "property", "value" }
                    }
                },
                new
                {
                    name = "basic10_simulator_get_device",
                    description = "Get a property from a simulated device (d0-d5)",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            deviceIndex = new
                            {
                                type = "integer",
                                description = "Device index (0-5)"
                            },
                            property = new
                            {
                                type = "string",
                                description = "Property name to read"
                            }
                        },
                        required = new[] { "deviceIndex", "property" }
                    }
                },
                // Debugging Tools
                new
                {
                    name = "basic10_add_watch",
                    description = "Add a watch expression to monitor a variable, register, or device property",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            expression = new
                            {
                                type = "string",
                                description = "Expression to watch (e.g., 'r0', 'myVar', 'd0.Temperature')"
                            }
                        },
                        required = new[] { "expression" }
                    }
                },
                new
                {
                    name = "basic10_get_watches",
                    description = "Get all watch expressions and their current values",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_clear_watches",
                    description = "Clear all watch expressions",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_get_errors",
                    description = "Get all compilation errors and warnings from the current code",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_get_sourcemap",
                    description = "Get the source map showing BASIC to IC10 line mappings and variable/alias mappings",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_goto_line",
                    description = "Navigate to a specific line in the BASIC editor",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            line = new
                            {
                                type = "integer",
                                description = "Line number to navigate to (1-based)"
                            }
                        },
                        required = new[] { "line" }
                    }
                },
                // Editor State Tools
                new
                {
                    name = "basic10_get_cursor",
                    description = "Get the current cursor position in the BASIC editor",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_set_cursor",
                    description = "Set the cursor position in the BASIC editor",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            line = new
                            {
                                type = "integer",
                                description = "Line number (1-based)"
                            },
                            column = new
                            {
                                type = "integer",
                                description = "Column number (1-based)"
                            }
                        },
                        required = new[] { "line", "column" }
                    }
                },
                new
                {
                    name = "basic10_insert_code",
                    description = "Insert code at the cursor position or a specific line",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            code = new
                            {
                                type = "string",
                                description = "The code to insert"
                            },
                            atLine = new
                            {
                                type = "integer",
                                description = "Optional: line number to insert at (1-based). If not specified, inserts at cursor."
                            }
                        },
                        required = new[] { "code" }
                    }
                },
                // Settings Tools
                new
                {
                    name = "basic10_get_settings",
                    description = "Get all current editor settings including theme, font size, auto-compile, etc.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "basic10_update_setting",
                    description = "Update a specific editor setting. Valid settings: theme (Dark/Light), fontSize, autoCompile, autoCompleteEnabled, wordWrap, optimizationLevel, autoSaveEnabled, autoSaveIntervalSeconds, splitViewMode (Vertical/Horizontal/EditorOnly), scriptAuthor",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new
                            {
                                type = "string",
                                description = "The name of the setting to update"
                            },
                            value = new
                            {
                                type = "string",
                                description = "The new value for the setting"
                            }
                        },
                        required = new[] { "name", "value" }
                    }
                },
                // Code Analysis Tools
                new
                {
                    name = "basic10_find_references",
                    description = "Find all references to a symbol (variable, constant, label, etc.) in the current code",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            symbolName = new
                            {
                                type = "string",
                                description = "The name of the symbol to find references for"
                            }
                        },
                        required = new[] { "symbolName" }
                    }
                },
                new
                {
                    name = "basic10_get_metrics",
                    description = "Get code metrics including line counts, symbol counts, and compilation status",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
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
            "basic10_format" => await CallFormat(),
            "basic10_get_symbols" => await CallGetSymbols(),
            "basic10_lookup_device" => await CallLookupDevice(arguments),
            "basic10_get_properties" => await CallGetProperties(arguments),
            "basic10_get_messages" => await CallGetMessages(),
            "basic10_send_response" => await CallSendResponse(arguments),
            // Tab Management
            "basic10_new_tab" => await CallNewTab(arguments),
            "basic10_list_tabs" => await CallListTabs(),
            "basic10_switch_tab" => await CallSwitchTab(arguments),
            "basic10_close_tab" => await CallCloseTab(arguments),
            // Script Save/Load
            "basic10_list_scripts" => await CallListScripts(),
            "basic10_save_script" => await CallSaveScript(arguments),
            "basic10_load_script" => await CallLoadScript(arguments),
            // Simulator
            "basic10_simulator_start" => await CallSimulatorStart(arguments),
            "basic10_simulator_stop" => await CallSimulatorStop(),
            "basic10_simulator_reset" => await CallSimulatorReset(),
            "basic10_simulator_step" => await CallSimulatorStep(),
            "basic10_simulator_run" => await CallSimulatorRun(arguments),
            "basic10_simulator_get_state" => await CallSimulatorGetState(),
            "basic10_simulator_set_register" => await CallSimulatorSetRegister(arguments),
            "basic10_simulator_add_breakpoint" => await CallSimulatorAddBreakpoint(arguments),
            "basic10_simulator_remove_breakpoint" => await CallSimulatorRemoveBreakpoint(arguments),
            "basic10_simulator_clear_breakpoints" => await CallSimulatorClearBreakpoints(),
            "basic10_simulator_set_device" => await CallSimulatorSetDevice(arguments),
            "basic10_simulator_get_device" => await CallSimulatorGetDevice(arguments),
            // Debugging
            "basic10_add_watch" => await CallAddWatch(arguments),
            "basic10_get_watches" => await CallGetWatches(),
            "basic10_clear_watches" => await CallClearWatches(),
            "basic10_get_errors" => await CallGetErrors(),
            "basic10_get_sourcemap" => await CallGetSourceMap(),
            "basic10_goto_line" => await CallGoToLine(arguments),
            // Editor State
            "basic10_get_cursor" => await CallGetCursor(),
            "basic10_set_cursor" => await CallSetCursor(arguments),
            "basic10_insert_code" => await CallInsertCode(arguments),
            // Settings
            "basic10_get_settings" => await CallGetSettings(),
            "basic10_update_setting" => await CallUpdateSetting(arguments),
            // Code Analysis
            "basic10_find_references" => await CallFindReferences(arguments),
            "basic10_get_metrics" => await CallGetMetrics(),
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

    private async Task<object> CallFormat()
    {
        var result = await _bridge.Format();
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

    // ==================== TAB MANAGEMENT ====================

    private async Task<object> CallNewTab(JsonNode? arguments)
    {
        var name = arguments?["name"]?.GetValue<string>();
        var result = await _bridge.NewTab(name);
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

    private async Task<object> CallListTabs()
    {
        var result = await _bridge.ListTabs();
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

    private async Task<object> CallSwitchTab(JsonNode? arguments)
    {
        var tabIndex = arguments?["tabIndex"]?.GetValue<int>();
        var tabName = arguments?["tabName"]?.GetValue<string>();

        var result = await _bridge.SwitchTab(tabIndex, tabName);
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

    private async Task<object> CallCloseTab(JsonNode? arguments)
    {
        var tabIndex = arguments?["tabIndex"]?.GetValue<int>();
        var force = arguments?["force"]?.GetValue<bool>() ?? false;

        if (tabIndex == null)
            throw new Exception("Missing 'tabIndex' argument");

        var result = await _bridge.CloseTab(tabIndex.Value, force);
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

    // ==================== SCRIPT SAVE/LOAD ====================

    private async Task<object> CallListScripts()
    {
        var result = await _bridge.ListScripts();
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

    private async Task<object> CallSaveScript(JsonNode? arguments)
    {
        var scriptName = arguments?["scriptName"]?.GetValue<string>();
        if (scriptName == null)
            throw new Exception("Missing 'scriptName' argument");

        var result = await _bridge.SaveScript(scriptName);
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

    private async Task<object> CallLoadScript(JsonNode? arguments)
    {
        var scriptName = arguments?["scriptName"]?.GetValue<string>();
        var newTab = arguments?["newTab"]?.GetValue<bool>() ?? false;

        if (scriptName == null)
            throw new Exception("Missing 'scriptName' argument");

        var result = await _bridge.LoadScript(scriptName, newTab);
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

    // ==================== SIMULATOR ====================

    private async Task<object> CallSimulatorStart(JsonNode? arguments)
    {
        var ic10Code = arguments?["ic10Code"]?.GetValue<string>();
        var result = await _bridge.SimulatorStart(ic10Code);
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

    private async Task<object> CallSimulatorStop()
    {
        var result = await _bridge.SimulatorStop();
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

    private async Task<object> CallSimulatorReset()
    {
        var result = await _bridge.SimulatorReset();
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

    private async Task<object> CallSimulatorStep()
    {
        var result = await _bridge.SimulatorStep();
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

    private async Task<object> CallSimulatorRun(JsonNode? arguments)
    {
        var maxInstructions = arguments?["maxInstructions"]?.GetValue<int>() ?? 10000;
        var result = await _bridge.SimulatorRun(maxInstructions);
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

    private async Task<object> CallSimulatorGetState()
    {
        var result = await _bridge.SimulatorGetState();
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

    private async Task<object> CallSimulatorSetRegister(JsonNode? arguments)
    {
        var register = arguments?["register"]?.GetValue<string>();
        var value = arguments?["value"]?.GetValue<double>() ?? 0;

        if (register == null)
            throw new Exception("Missing 'register' argument");

        var result = await _bridge.SimulatorSetRegister(register, value);
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

    private async Task<object> CallSimulatorAddBreakpoint(JsonNode? arguments)
    {
        var line = arguments?["line"]?.GetValue<int>() ?? 0;
        var result = await _bridge.SimulatorAddBreakpoint(line);
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

    private async Task<object> CallSimulatorRemoveBreakpoint(JsonNode? arguments)
    {
        var line = arguments?["line"]?.GetValue<int>() ?? 0;
        var result = await _bridge.SimulatorRemoveBreakpoint(line);
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

    private async Task<object> CallSimulatorClearBreakpoints()
    {
        var result = await _bridge.SimulatorClearBreakpoints();
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

    private async Task<object> CallSimulatorSetDevice(JsonNode? arguments)
    {
        var deviceIndex = arguments?["deviceIndex"]?.GetValue<int>() ?? 0;
        var property = arguments?["property"]?.GetValue<string>();
        var value = arguments?["value"]?.GetValue<double>() ?? 0;

        if (property == null)
            throw new Exception("Missing 'property' argument");

        var result = await _bridge.SimulatorSetDevice(deviceIndex, property, value);
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

    private async Task<object> CallSimulatorGetDevice(JsonNode? arguments)
    {
        var deviceIndex = arguments?["deviceIndex"]?.GetValue<int>() ?? 0;
        var property = arguments?["property"]?.GetValue<string>();

        if (property == null)
            throw new Exception("Missing 'property' argument");

        var result = await _bridge.SimulatorGetDevice(deviceIndex, property);
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

    // ==================== DEBUGGING ====================

    private async Task<object> CallAddWatch(JsonNode? arguments)
    {
        var expression = arguments?["expression"]?.GetValue<string>();
        if (expression == null)
            throw new Exception("Missing 'expression' argument");

        var result = await _bridge.AddWatch(expression);
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

    private async Task<object> CallGetWatches()
    {
        var result = await _bridge.GetWatches();
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

    private async Task<object> CallClearWatches()
    {
        var result = await _bridge.ClearWatches();
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

    private async Task<object> CallGetErrors()
    {
        var result = await _bridge.GetErrors();
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

    private async Task<object> CallGetSourceMap()
    {
        var result = await _bridge.GetSourceMap();
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

    private async Task<object> CallGoToLine(JsonNode? arguments)
    {
        var line = arguments?["line"]?.GetValue<int>() ?? 1;
        var result = await _bridge.GoToLine(line);
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

    #region Editor State Call Handlers

    private async Task<object> CallGetCursor()
    {
        var result = await _bridge.GetCursor();
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

    private async Task<object> CallSetCursor(JsonNode? arguments)
    {
        var line = arguments?["line"]?.GetValue<int>() ?? 1;
        var column = arguments?["column"]?.GetValue<int>() ?? 1;
        var result = await _bridge.SetCursor(line, column);
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

    private async Task<object> CallInsertCode(JsonNode? arguments)
    {
        var code = arguments?["code"]?.GetValue<string>() ?? "";
        int? atLine = null;
        if (arguments?["atLine"] != null)
            atLine = arguments["atLine"]!.GetValue<int>();

        var result = await _bridge.InsertCode(code, atLine);
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

    #endregion

    #region Settings Call Handlers

    private async Task<object> CallGetSettings()
    {
        var result = await _bridge.GetSettings();
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

    private async Task<object> CallUpdateSetting(JsonNode? arguments)
    {
        var name = arguments?["name"]?.GetValue<string>() ?? "";
        var value = arguments?["value"]?.GetValue<string>() ?? "";
        var result = await _bridge.UpdateSetting(name, value);
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

    #endregion

    #region Code Analysis Call Handlers

    private async Task<object> CallFindReferences(JsonNode? arguments)
    {
        var symbolName = arguments?["symbolName"]?.GetValue<string>() ?? "";
        var result = await _bridge.FindReferences(symbolName);
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

    private async Task<object> CallGetMetrics()
    {
        var result = await _bridge.GetCodeMetrics();
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

    #endregion

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
