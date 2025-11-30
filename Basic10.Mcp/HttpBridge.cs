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
}

#region Response Models

internal class CodeResponse
{
    public string? Code { get; set; }
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

#endregion
