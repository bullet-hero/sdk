using BH.SDK;
using BH.SDK.Agent;
using BH.SDK.Mcp;
using BH.SDK.Mcp.Prompts;
using BH.SDK.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

// stdout belongs to the MCP transport: every log line goes to stderr.
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

var endpoint = EndpointArgument(args) ?? AgentEndpoint.DefaultPath();
builder.Services.AddSingleton(new BridgeClient(endpoint));

// Parameter types are resolved from source-generated metadata: a trimmed binary has no reflection JSON.
var toolJson = new JsonSerializerOptions(McpJsonUtilities.DefaultOptions);
toolJson.TypeInfoResolverChain.Add(ToolJsonContext.Default);

// Every tool class is listed: WithToolsFromAssembly() finds nothing once the binary is trimmed.
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation { Name = "bullet-hero", Version = SdkVersion.Value };
        options.ServerInstructions = ServerInstructions.Text;
    })
    .WithStdioServerTransport()
    .WithTools<SessionTools>(toolJson)
    .WithTools<LevelTools>(toolJson)
    .WithTools<ReadTools>(toolJson)
    .WithTools<EditTools>(toolJson)
    .WithTools<HistoryTools>(toolJson)
    .WithTools<SelectionTools>(toolJson)
    .WithTools<PrefabTools>(toolJson)
    .WithTools<GeneratorTools>(toolJson)
    .WithTools<MaintenanceTools>(toolJson)
    .WithTools<VisionTools>(toolJson)
    .WithTools<CaptureTools>(toolJson)
    .WithTools<MusicTools>(toolJson)
    .WithTools<ContextTools>(toolJson)
    .WithPrompts<WorkflowPrompts>(toolJson);

await builder.Build().RunAsync();

static string EndpointArgument(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == "--endpoint") return args[i + 1];

    return null;
}
