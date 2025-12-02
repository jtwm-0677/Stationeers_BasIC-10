using System.Net.Http.Json;
using System.Text.Json;

namespace Basic10.Mcp;

/// <summary>
/// HTTP client bridge to communicate with Basic-10's HTTP API.
/// </summary>
public class HttpBridge
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpBridge(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Get the current BASIC code from the editor.
    /// </summary>
    public async Task<string> GetCode()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/code");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CodeResponse>(_jsonOptions);
            return result?.Code ?? "";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Set the BASIC code in the editor.
    /// </summary>
    public async Task SetCode(string code)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/code", new { code }, _jsonOptions);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Compile the current code and return formatted results.
    /// </summary>
    public async Task<string> Compile()
    {
        try
        {
            var response = await _client.PostAsync($"{_baseUrl}/api/compile", null);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CompileResponse>(content, _jsonOptions);

            if (result == null)
                return "Compilation failed: No response";

            var output = new System.Text.StringBuilder();

            if (result.Success)
            {
                output.AppendLine($"Compilation successful ({result.LineCount} IC10 lines)");
                output.AppendLine();
                output.AppendLine("IC10 Output:");
                output.AppendLine("```ic10");
                output.AppendLine(result.Ic10Output);
                output.AppendLine("```");
            }
            else
            {
                output.AppendLine("Compilation failed");
            }

            if (result.Errors?.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("Errors/Warnings:");
                foreach (var error in result.Errors)
                {
                    var severity = error.Severity ?? "error";
                    output.AppendLine($"  [{severity}] Line {error.Line}: {error.Message}");
                }
            }

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Get the symbol table from the current code.
    /// </summary>
    public async Task<string> GetSymbols()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/symbols");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SymbolTableResponse>(content, _jsonOptions);

            if (result == null)
                return "No symbols found";

            var output = new System.Text.StringBuilder();
            output.AppendLine("Symbol Table:");

            if (result.Variables?.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("Variables:");
                foreach (var v in result.Variables)
                    output.AppendLine($"  {v}");
            }

            if (result.Labels?.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("Labels:");
                foreach (var l in result.Labels)
                    output.AppendLine($"  {l}:");
            }

            if (result.Aliases?.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("Aliases (Device Types):");
                foreach (var (name, type) in result.Aliases)
                    output.AppendLine($"  {name} -> {type}");
            }

            if (result.Constants?.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("Constants:");
                foreach (var (name, value) in result.Constants)
                    output.AppendLine($"  {name} = {value}");
            }

            if (result.Functions?.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("Functions/Subroutines:");
                foreach (var (name, paramCount) in result.Functions)
                    output.AppendLine($"  {name}({paramCount} params)");
            }

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Look up devices by name or hash.
    /// </summary>
    public async Task<string> LookupDevice(string? query, int? hash)
    {
        try
        {
            var url = $"{_baseUrl}/api/devices";
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(query))
                queryParams.Add($"query={Uri.EscapeDataString(query)}");
            if (hash.HasValue)
                queryParams.Add($"hash={hash.Value}");

            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DeviceListResponse>(content, _jsonOptions);

            if (result?.Devices == null || result.Devices.Count == 0)
                return "No devices found matching the query.";

            var output = new System.Text.StringBuilder();
            output.AppendLine($"Found {result.Count} device(s):");
            output.AppendLine();

            foreach (var device in result.Devices.Take(20)) // Limit to 20 results
            {
                output.AppendLine($"  {device.DisplayName}");
                output.AppendLine($"    PrefabName: {device.PrefabName}");
                output.AppendLine($"    Category: {device.Category}");
                output.AppendLine($"    Hash: {device.Hash}");
                if (!string.IsNullOrEmpty(device.Description))
                    output.AppendLine($"    Description: {device.Description}");
                output.AppendLine();
            }

            if (result.Count > 20)
                output.AppendLine($"  ... and {result.Count - 20} more results");

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Get logic properties list.
    /// </summary>
    public async Task<string> GetProperties(string? query)
    {
        try
        {
            var url = $"{_baseUrl}/api/properties";
            if (!string.IsNullOrEmpty(query))
                url += $"?query={Uri.EscapeDataString(query)}";

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PropertyListResponse>(content, _jsonOptions);

            if (result?.Properties == null || result.Properties.Count == 0)
                return "No properties found.";

            var output = new System.Text.StringBuilder();
            output.AppendLine($"Found {result.Count} logic properties:");
            output.AppendLine();

            foreach (var prop in result.Properties.Take(50)) // Limit to 50
            {
                output.AppendLine($"  {prop.Name} (Hash: {prop.Hash}, Value: {prop.Value})");
                if (!string.IsNullOrEmpty(prop.Description))
                    output.AppendLine($"    {prop.Description}");
            }

            if (result.Count > 50)
                output.AppendLine($"  ... and {result.Count - 50} more properties");

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Get pending messages from the user (via Claude Assistant window).
    /// </summary>
    public async Task<string> GetMessages()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/messages");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MessagesResponse>(content, _jsonOptions);

            if (result?.Messages == null || result.Messages.Count == 0)
                return "No pending messages.";

            var output = new System.Text.StringBuilder();
            output.AppendLine($"Received {result.Count} message(s) from user:");
            output.AppendLine();

            foreach (var msg in result.Messages)
            {
                output.AppendLine($"[{msg.Id}] {msg.Content}");
            }

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Send a response back to the Claude Assistant window.
    /// </summary>
    public async Task<string> SendResponse(string content, string? messageId = null)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/messages/response",
                new { content, messageId }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            return "Response sent to Claude Assistant window.";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    // ==================== TAB MANAGEMENT ====================

    /// <summary>
    /// Create a new tab in the editor.
    /// </summary>
    public async Task<string> NewTab(string? name = null)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/tabs",
                new { name }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<NewTabResponse>(content, _jsonOptions);

            return $"Created new tab at index {result?.TabIndex ?? 0}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// List all open tabs.
    /// </summary>
    public async Task<string> ListTabs()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/tabs");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TabListResponse>(content, _jsonOptions);

            if (result?.Tabs == null || result.Tabs.Count == 0)
                return "No tabs open.";

            var output = new System.Text.StringBuilder();
            output.AppendLine($"Open tabs ({result.Count}):");
            output.AppendLine();

            foreach (var tab in result.Tabs)
            {
                var active = tab.IsActive ? " [ACTIVE]" : "";
                var modified = tab.IsModified ? " *" : "";
                output.AppendLine($"  [{tab.Index}] {tab.Name}{modified}{active}");
                if (!string.IsNullOrEmpty(tab.FilePath))
                    output.AppendLine($"      Path: {tab.FilePath}");
            }

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Switch to a specific tab.
    /// </summary>
    public async Task<string> SwitchTab(int? tabIndex, string? tabName)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/tabs/switch",
                new { tabIndex, tabName }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SuccessResponse>(content, _jsonOptions);

            if (result?.Success == true)
                return $"Switched to tab {tabIndex?.ToString() ?? tabName}";
            else
                return $"Failed to switch to tab {tabIndex?.ToString() ?? tabName}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Close a specific tab.
    /// </summary>
    /// <param name="tabIndex">Index of the tab to close</param>
    /// <param name="force">If true, close without prompting to save unsaved changes</param>
    public async Task<string> CloseTab(int tabIndex, bool force = false)
    {
        try
        {
            var url = $"{_baseUrl}/api/tabs/{tabIndex}";
            if (force)
                url += "?force=true";

            var response = await _client.DeleteAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SuccessResponse>(content, _jsonOptions);

            if (result?.Success == true)
                return $"Closed tab {tabIndex}" + (force ? " (forced)" : "");
            else
                return $"Failed to close tab {tabIndex}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    // ==================== SCRIPT SAVE/LOAD ====================

    /// <summary>
    /// List all scripts in the scripts folder.
    /// </summary>
    public async Task<string> ListScripts()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/scripts");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ScriptListResponse>(content, _jsonOptions);

            if (result?.Scripts == null || result.Scripts.Count == 0)
                return "No scripts found in scripts folder.";

            var output = new System.Text.StringBuilder();
            output.AppendLine($"Available scripts ({result.Count}):");
            output.AppendLine();

            foreach (var script in result.Scripts)
            {
                var bas = script.HasBasFile ? "✓" : "✗";
                var xml = script.HasInstructionXml ? "✓" : "✗";
                output.AppendLine($"  {script.Name}  (bas:{bas} xml:{xml})");
            }

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Save the current script to the scripts folder.
    /// </summary>
    public async Task<string> SaveScript(string scriptName)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/scripts/save",
                new { scriptName }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SaveScriptResponse>(content, _jsonOptions);

            if (result?.Success == true)
                return $"Script saved to folder: {result.FolderPath}";
            else
                return $"Failed to save script: {result?.Error ?? "Unknown error"}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Load a script from the scripts folder.
    /// </summary>
    public async Task<string> LoadScript(string scriptName, bool newTab = false)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/scripts/load",
                new { scriptName, newTab }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoadScriptResponse>(content, _jsonOptions);

            if (result?.Success == true)
            {
                var output = new System.Text.StringBuilder();
                output.AppendLine($"Loaded script: {result.ScriptName}");
                output.AppendLine($"File: {result.FilePath}");
                if (!string.IsNullOrEmpty(result.CodePreview))
                {
                    output.AppendLine();
                    output.AppendLine("Preview:");
                    output.AppendLine(result.CodePreview);
                    if (result.CodePreview.Length >= 500)
                        output.AppendLine("...");
                }
                return output.ToString();
            }
            else
                return $"Failed to load script: {result?.Error ?? "Unknown error"}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    // ==================== SIMULATOR ====================

    /// <summary>
    /// Start/initialize the IC10 simulator.
    /// </summary>
    public async Task<string> SimulatorStart(string? ic10Code = null)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/simulator/start",
                new { ic10Code }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, "Simulator started");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Stop the IC10 simulator.
    /// </summary>
    public async Task<string> SimulatorStop()
    {
        try
        {
            var response = await _client.PostAsync($"{_baseUrl}/api/simulator/stop", null);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, "Simulator stopped");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Reset the IC10 simulator.
    /// </summary>
    public async Task<string> SimulatorReset()
    {
        try
        {
            var response = await _client.PostAsync($"{_baseUrl}/api/simulator/reset", null);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, "Simulator reset");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Execute a single IC10 instruction.
    /// </summary>
    public async Task<string> SimulatorStep()
    {
        try
        {
            var response = await _client.PostAsync($"{_baseUrl}/api/simulator/step", null);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, "Stepped");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Run the simulator until breakpoint, halt, or yield.
    /// </summary>
    public async Task<string> SimulatorRun(int maxInstructions = 10000)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/simulator/run",
                new { maxInstructions }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, "Run complete");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Get current simulator state.
    /// </summary>
    public async Task<string> SimulatorGetState()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/simulator/state");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, "Current state");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Set a register value.
    /// </summary>
    public async Task<string> SimulatorSetRegister(string register, double value)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/simulator/register",
                new { register, value }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, $"Set {register} = {value}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Add a breakpoint.
    /// </summary>
    public async Task<string> SimulatorAddBreakpoint(int line)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/simulator/breakpoint",
                new { line }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, $"Breakpoint added at line {line}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Remove a breakpoint.
    /// </summary>
    public async Task<string> SimulatorRemoveBreakpoint(int line)
    {
        try
        {
            var response = await _client.DeleteAsync($"{_baseUrl}/api/simulator/breakpoint/{line}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, $"Breakpoint removed from line {line}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Clear all breakpoints.
    /// </summary>
    public async Task<string> SimulatorClearBreakpoints()
    {
        try
        {
            var response = await _client.DeleteAsync($"{_baseUrl}/api/simulator/breakpoints");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, "All breakpoints cleared");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Set a simulated device property.
    /// </summary>
    public async Task<string> SimulatorSetDevice(int deviceIndex, string property, double value)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/simulator/device",
                new { deviceIndex, property, value }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SimulatorStateResponse>(content, _jsonOptions);

            return FormatSimulatorState(result, $"Set d{deviceIndex}.{property} = {value}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Get a simulated device property.
    /// </summary>
    public async Task<string> SimulatorGetDevice(int deviceIndex, string property)
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/simulator/device/{deviceIndex}/{Uri.EscapeDataString(property)}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DevicePropertyResponse>(content, _jsonOptions);

            return $"d{result?.DeviceIndex ?? deviceIndex}.{result?.Property ?? property} = {result?.Value ?? 0}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    // ==================== DEBUGGING ====================

    /// <summary>
    /// Add a watch expression.
    /// </summary>
    public async Task<string> AddWatch(string expression)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/debug/watch",
                new { expression }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WatchInfoResponse>(content, _jsonOptions);

            return $"Added watch: {result?.Name} = {result?.Value} (Type: {result?.Type})";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Get all watch values.
    /// </summary>
    public async Task<string> GetWatches()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/debug/watches");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WatchListResponse>(content, _jsonOptions);

            if (result?.Watches == null || result.Watches.Count == 0)
                return "No watches defined.";

            var output = new System.Text.StringBuilder();
            output.AppendLine($"Watch Variables ({result.Count}):");
            foreach (var watch in result.Watches)
            {
                var changed = watch.HasChanged ? " *" : "";
                output.AppendLine($"  {watch.Name} = {watch.Value}{changed} ({watch.Type})");
            }
            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Clear all watches.
    /// </summary>
    public async Task<string> ClearWatches()
    {
        try
        {
            var response = await _client.DeleteAsync($"{_baseUrl}/api/debug/watches");
            response.EnsureSuccessStatusCode();
            return "All watches cleared.";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Get compilation errors.
    /// </summary>
    public async Task<string> GetErrors()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/debug/errors");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ErrorListResponse>(content, _jsonOptions);

            if (result?.Errors == null || result.Errors.Count == 0)
                return "No errors or warnings.";

            var output = new System.Text.StringBuilder();
            output.AppendLine($"Compilation Issues ({result.Count}):");
            foreach (var error in result.Errors)
            {
                var prefix = error.Severity == "error" ? "ERROR" : "WARN";
                output.AppendLine($"  [{prefix}] Line {error.Line}: {error.Message}");
            }
            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Get the source map.
    /// </summary>
    public async Task<string> GetSourceMap()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/debug/sourcemap");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SourceMapResponse>(content, _jsonOptions);

            if (result?.HasMap != true)
                return "No source map available. Compile the code first.";

            var output = new System.Text.StringBuilder();
            output.AppendLine("Source Map:");

            if (result.BasicToIc10 != null && result.BasicToIc10.Count > 0)
            {
                output.AppendLine("\nLine Mappings (BASIC -> IC10):");
                foreach (var mapping in result.BasicToIc10.Take(20))
                {
                    var ic10Lines = string.Join(", ", mapping.Value);
                    output.AppendLine($"  Line {mapping.Key} -> IC10 lines [{ic10Lines}]");
                }
                if (result.BasicToIc10.Count > 20)
                    output.AppendLine($"  ... and {result.BasicToIc10.Count - 20} more mappings");
            }

            if (result.Ic10ToBasic != null && result.Ic10ToBasic.Count > 0)
            {
                output.AppendLine("\nReverse Mappings (IC10 -> BASIC):");
                foreach (var mapping in result.Ic10ToBasic.Take(20))
                {
                    output.AppendLine($"  IC10 line {mapping.Key} -> BASIC line {mapping.Value}");
                }
                if (result.Ic10ToBasic.Count > 20)
                    output.AppendLine($"  ... and {result.Ic10ToBasic.Count - 20} more mappings");
            }

            if (result.VariableRegisters != null && result.VariableRegisters.Count > 0)
            {
                output.AppendLine("\nVariable Registers:");
                foreach (var mapping in result.VariableRegisters)
                {
                    output.AppendLine($"  {mapping.Key} -> {mapping.Value}");
                }
            }

            if (result.AliasDevices != null && result.AliasDevices.Count > 0)
            {
                output.AppendLine("\nDevice Aliases:");
                foreach (var mapping in result.AliasDevices)
                {
                    output.AppendLine($"  {mapping.Key} -> {mapping.Value}");
                }
            }

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Navigate to a line.
    /// </summary>
    public async Task<string> GoToLine(int line)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/debug/goto",
                new { line }, _jsonOptions);
            response.EnsureSuccessStatusCode();
            return $"Navigated to line {line}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    #region Editor State Methods

    /// <summary>
    /// Get the current cursor position.
    /// </summary>
    public async Task<string> GetCursor()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/cursor");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CursorResponse>(content, _jsonOptions);

            return $"Cursor position: Line {result?.Line ?? 0}, Column {result?.Column ?? 0}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Set the cursor position.
    /// </summary>
    public async Task<string> SetCursor(int line, int column)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/cursor",
                new { line, column }, _jsonOptions);
            response.EnsureSuccessStatusCode();
            return $"Cursor moved to line {line}, column {column}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Insert code at cursor or specific line.
    /// </summary>
    public async Task<string> InsertCode(string code, int? atLine = null)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, $"{_baseUrl}/api/code")
            {
                Content = JsonContent.Create(new { code, atLine }, options: _jsonOptions)
            };
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            if (atLine.HasValue)
                return $"Inserted code at line {atLine.Value}";
            return "Inserted code at cursor position";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    #endregion

    #region Settings Methods

    /// <summary>
    /// Get all current settings.
    /// </summary>
    public async Task<string> GetSettings()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/settings");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SettingsResponse>(content, _jsonOptions);

            if (result == null)
                return "No settings available";

            var output = new System.Text.StringBuilder();
            output.AppendLine("=== Current Settings ===");
            output.AppendLine($"Theme: {result.Theme}");
            output.AppendLine($"Font Size: {result.FontSize}");
            output.AppendLine($"Auto-Compile: {result.AutoCompile}");
            output.AppendLine($"Auto-Complete: {result.AutoCompleteEnabled}");
            output.AppendLine($"Word Wrap: {result.WordWrap}");
            output.AppendLine($"Optimization Level: {result.OptimizationLevel}");
            output.AppendLine($"Auto-Save: {result.AutoSaveEnabled}");
            output.AppendLine($"Auto-Save Interval: {result.AutoSaveIntervalSeconds}s");
            output.AppendLine($"Split View Mode: {result.SplitViewMode}");
            output.AppendLine($"API Server: {(result.ApiServerEnabled ? $"Enabled (port {result.ApiServerPort})" : "Disabled")}");
            output.AppendLine($"Script Author: {(string.IsNullOrEmpty(result.ScriptAuthor) ? "(not set)" : result.ScriptAuthor)}");

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Update a setting.
    /// </summary>
    public async Task<string> UpdateSetting(string name, object value)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/api/settings")
            {
                Content = JsonContent.Create(new { name, value }, options: _jsonOptions)
            };
            var response = await _client.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SettingsUpdateResponse>(content, _jsonOptions);

            if (result?.Success == true)
                return $"Setting '{name}' updated successfully";
            else
                return $"Failed to update setting: {result?.Error ?? "Unknown error"}";
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    #endregion

    #region Code Analysis Methods

    /// <summary>
    /// Find all references to a symbol.
    /// </summary>
    public async Task<string> FindReferences(string symbolName)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/analysis/references",
                new { symbolName }, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<FindReferencesResponse>(content, _jsonOptions);

            if (result?.References == null || result.References.Count == 0)
                return $"No references found for '{symbolName}'";

            var output = new System.Text.StringBuilder();
            output.AppendLine($"=== References to '{symbolName}' ({result.Count} found) ===");

            foreach (var r in result.References)
            {
                output.AppendLine($"  Line {r.Line}, Column {r.Column} ({r.Kind})");
            }

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    /// <summary>
    /// Get code metrics.
    /// </summary>
    public async Task<string> GetCodeMetrics()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUrl}/api/analysis/metrics");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CodeMetricsResponse>(content, _jsonOptions);

            if (result == null)
                return "No metrics available";

            var output = new System.Text.StringBuilder();
            output.AppendLine("=== Code Metrics ===");
            output.AppendLine($"Total Lines: {result.TotalLines}");
            output.AppendLine($"  Code Lines: {result.CodeLines}");
            output.AppendLine($"  Comment Lines: {result.CommentLines}");
            output.AppendLine($"  Blank Lines: {result.BlankLines}");
            output.AppendLine();
            output.AppendLine($"IC10 Output: {result.Ic10Lines} lines");
            output.AppendLine($"Compilation: {(result.CompilationSuccess ? "Success" : "Failed")}");
            if (result.WarningCount > 0 || result.ErrorCount > 0)
                output.AppendLine($"  Errors: {result.ErrorCount}, Warnings: {result.WarningCount}");
            output.AppendLine();
            output.AppendLine($"Symbols:");
            output.AppendLine($"  Variables: {result.VariableCount}");
            output.AppendLine($"  Constants: {result.ConstantCount}");
            output.AppendLine($"  Labels: {result.LabelCount}");
            output.AppendLine($"  Functions: {result.FunctionCount}");
            output.AppendLine($"  Aliases: {result.AliasCount}");

            return output.ToString();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to connect to Basic-10: {ex.Message}. Make sure Basic-10 is running.");
        }
    }

    #endregion

    private string FormatSimulatorState(SimulatorStateResponse? state, string header)
    {
        if (state == null)
            return $"{header}: No response";

        if (!state.Success)
            return $"{header}: {state.Error ?? "Unknown error"}";

        var output = new System.Text.StringBuilder();
        output.AppendLine($"=== {header} ===");
        output.AppendLine($"PC: {state.ProgramCounter} | Instructions: {state.InstructionCount}");
        output.AppendLine($"Status: {(state.IsHalted ? "HALTED" : state.IsYielding ? "YIELDING" : state.IsPaused ? "PAUSED" : "READY")}");

        if (!string.IsNullOrEmpty(state.ErrorMessage))
            output.AppendLine($"Error: {state.ErrorMessage}");

        // Show non-zero registers
        if (state.Registers != null && state.Registers.Count > 0)
        {
            var nonZero = state.Registers.Where(r => Math.Abs(r.Value) > 0.0001).ToList();
            if (nonZero.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("Registers:");
                foreach (var reg in nonZero)
                {
                    output.AppendLine($"  {reg.Key} = {reg.Value:F4}");
                }
            }
        }

        // Show stack if not empty
        if (state.Stack != null && state.Stack.Length > 0)
        {
            output.AppendLine();
            output.AppendLine($"Stack ({state.Stack.Length} items):");
            for (int i = state.Stack.Length - 1; i >= Math.Max(0, state.Stack.Length - 5); i--)
            {
                output.AppendLine($"  [{i}] = {state.Stack[i]:F4}");
            }
            if (state.Stack.Length > 5)
                output.AppendLine($"  ... {state.Stack.Length - 5} more items");
        }

        // Show breakpoints
        if (state.Breakpoints != null && state.Breakpoints.Count > 0)
        {
            output.AppendLine();
            output.AppendLine($"Breakpoints: {string.Join(", ", state.Breakpoints)}");
        }

        return output.ToString();
    }
}

#region Response Models

internal class CodeResponse
{
    public string? Code { get; set; }
}

internal class CursorResponse
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Offset { get; set; }
}

internal class CompileResponse
{
    public bool Success { get; set; }
    public string? Ic10Output { get; set; }
    public int LineCount { get; set; }
    public List<CompileError>? Errors { get; set; }
}

internal class CompileError
{
    public string? Message { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public string? Severity { get; set; }
}

internal class SymbolTableResponse
{
    public List<string>? Variables { get; set; }
    public List<string>? Labels { get; set; }
    public Dictionary<string, string>? Aliases { get; set; }
    public Dictionary<string, double>? Constants { get; set; }
    public Dictionary<string, int>? Functions { get; set; }
}

internal class DeviceListResponse
{
    public List<DeviceInfo>? Devices { get; set; }
    public int Count { get; set; }
}

internal class DeviceInfo
{
    public string? PrefabName { get; set; }
    public string? DisplayName { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public int Hash { get; set; }
}

internal class PropertyListResponse
{
    public List<PropertyInfo>? Properties { get; set; }
    public int Count { get; set; }
}

internal class PropertyInfo
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int Hash { get; set; }
    public int Value { get; set; }
}

internal class MessagesResponse
{
    public List<MessageInfo>? Messages { get; set; }
    public int Count { get; set; }
}

internal class MessageInfo
{
    public string? Id { get; set; }
    public string? Content { get; set; }
    public string? Timestamp { get; set; }
}

// Tab Management
internal class NewTabResponse
{
    public bool Success { get; set; }
    public int TabIndex { get; set; }
}

internal class TabListResponse
{
    public List<TabInfo>? Tabs { get; set; }
    public int Count { get; set; }
}

internal class TabInfo
{
    public int Index { get; set; }
    public string? Name { get; set; }
    public string? FilePath { get; set; }
    public bool IsModified { get; set; }
    public bool IsActive { get; set; }
}

internal class SuccessResponse
{
    public bool Success { get; set; }
}

// Script Save/Load
internal class ScriptListResponse
{
    public List<ScriptInfo>? Scripts { get; set; }
    public int Count { get; set; }
}

internal class ScriptInfo
{
    public string? Name { get; set; }
    public string? FolderPath { get; set; }
    public bool HasBasFile { get; set; }
    public bool HasInstructionXml { get; set; }
}

internal class SaveScriptResponse
{
    public bool Success { get; set; }
    public string? ScriptName { get; set; }
    public string? FolderPath { get; set; }
    public string? Error { get; set; }
}

internal class LoadScriptResponse
{
    public bool Success { get; set; }
    public string? ScriptName { get; set; }
    public string? FilePath { get; set; }
    public string? CodePreview { get; set; }
    public string? Error { get; set; }
}

// Simulator
internal class SimulatorStateResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int ProgramCounter { get; set; }
    public int InstructionCount { get; set; }
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public bool IsHalted { get; set; }
    public bool IsYielding { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, double>? Registers { get; set; }
    public double[]? Stack { get; set; }
    public List<SimulatorDeviceInfo>? Devices { get; set; }
    public List<int>? Breakpoints { get; set; }
}

internal class SimulatorDeviceInfo
{
    public int Index { get; set; }
    public string? Name { get; set; }
    public string? Alias { get; set; }
    public Dictionary<string, double>? Properties { get; set; }
}

internal class DevicePropertyResponse
{
    public int DeviceIndex { get; set; }
    public string? Property { get; set; }
    public double Value { get; set; }
}

// Debugging
internal class WatchInfoResponse
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? Type { get; set; }
    public bool HasChanged { get; set; }
}

internal class WatchListResponse
{
    public List<WatchInfoResponse>? Watches { get; set; }
    public int Count { get; set; }
}

internal class ErrorInfoResponse
{
    public int Line { get; set; }
    public string? Message { get; set; }
    public string? Severity { get; set; }
}

internal class ErrorListResponse
{
    public List<ErrorInfoResponse>? Errors { get; set; }
    public int Count { get; set; }
}

internal class SourceMapResponse
{
    public bool HasMap { get; set; }
    public Dictionary<int, List<int>>? BasicToIc10 { get; set; }
    public Dictionary<int, int>? Ic10ToBasic { get; set; }
    public Dictionary<string, string>? VariableRegisters { get; set; }
    public Dictionary<string, string>? AliasDevices { get; set; }
}

internal class SettingsResponse
{
    public string? Theme { get; set; }
    public double FontSize { get; set; }
    public bool AutoCompile { get; set; }
    public bool AutoCompleteEnabled { get; set; }
    public bool WordWrap { get; set; }
    public int OptimizationLevel { get; set; }
    public bool AutoSaveEnabled { get; set; }
    public int AutoSaveIntervalSeconds { get; set; }
    public string? SplitViewMode { get; set; }
    public bool ApiServerEnabled { get; set; }
    public int ApiServerPort { get; set; }
    public string? ScriptAuthor { get; set; }
}

internal class SettingsUpdateResponse
{
    public bool Success { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
}

internal class SymbolReferenceResponse
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Length { get; set; }
    public string? Kind { get; set; }
}

internal class FindReferencesResponse
{
    public List<SymbolReferenceResponse>? References { get; set; }
    public int Count { get; set; }
}

internal class CodeMetricsResponse
{
    public int TotalLines { get; set; }
    public int CodeLines { get; set; }
    public int CommentLines { get; set; }
    public int BlankLines { get; set; }
    public int Ic10Lines { get; set; }
    public bool CompilationSuccess { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public int VariableCount { get; set; }
    public int ConstantCount { get; set; }
    public int LabelCount { get; set; }
    public int FunctionCount { get; set; }
    public int AliasCount { get; set; }
}

#endregion
