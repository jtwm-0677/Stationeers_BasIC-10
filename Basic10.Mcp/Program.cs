namespace Basic10.Mcp;

/// <summary>
/// MCP Server entry point.
/// This console app is spawned by Claude Code and communicates via stdio.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Get API port from environment or use default
        var portStr = Environment.GetEnvironmentVariable("BASIC10_API_PORT");
        var port = int.TryParse(portStr, out var p) ? p : 19410;

        var httpBridge = new HttpBridge($"http://localhost:{port}");
        var server = new McpServer(httpBridge);

        await server.Run();
    }
}
