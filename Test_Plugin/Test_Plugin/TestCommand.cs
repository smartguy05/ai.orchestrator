using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Models;
using Ai.Orchestrator.Models.Enums;
using Ai.Orchestrator.Models.Extensions;
using Ai.Orchestrator.Models.Tools;

namespace Test_Plugin;

public class TestCommand : ICommand
{
    public string Name => "TEST";
    public string Description  => "Displays hello message.";
    public LogDelegate Logger { get; set; }

    // Example service request of string array
    // public async Task<object> Execute(object request, string config)
    // {
    //     var args = request.GetServiceRequestArray<string>().ToList();
            // var config = configString.ReadConfig<TestConfig>();
    //     
    //     Console.WriteLine("TEST SUCCESSFUL!!!");
    //     if (args.Any())
    //     {
    //         for (var i = 0; i < args.Count; i++)
    //         {
    //             Console.WriteLine($"Param {i+1}");
    //             Console.WriteLine(args[i]);
    //         }
    //     }
    //     else
    //     {
    //         Console.WriteLine("No args supplied.");
    //     }
    // Console.WriteLine("Config");
    // Console.WriteLine($"TestName: {config.TestName}");
    // Console.WriteLine($"TestName2: {config.TestName2}");
    //
    //     return Task.FromResult(Task.FromResult((object)0));
    // }
    
    public Task<object> Execute(OrchestratorRequest request, string configString, 
        IEnumerable<ToolCall> availableToolCalls, LogDelegate logFunction)
    {
        Logger = logFunction;
        var args = request.ServiceRequest as TestClass;
        // var args = request.GetServiceRequest<TestClass>();
        var config = configString.ReadPluginConfig<TestConfig>();

        Console.WriteLine("TEST SUCCESSFUL!!!");
        if (args is not null)
        {
            Console.WriteLine($"Arg Name: {args.Name}");
            Console.WriteLine($"Arg Value: {args.Value}");
        }
        else
        {
            Console.WriteLine("No args supplied.");
        }
        
        Console.WriteLine("Config");
        Console.WriteLine($"TestName: {config.TestName}");
        Console.WriteLine($"TestName2: {config.TestName2}");

        return Task.FromResult((object)0);
    }
    
    public virtual async Task Log(LogLevel logLevel, string message, Exception exception = null)
    {
        await Logger(logLevel, message, exception);
    }
    
    public Task<object> Initialize(string config, LogDelegate logFunction)
    {
        return Task.FromResult<object>(null);
    }
}

public class TestClass
{
    public string Name { get; set; }
    public int Value { get; set; }
}

public class TestConfig: IPluginConfig
{
    public string Name { get; set; }
    public string Description { get; set; }
    public IEnumerable<ToolCall> Tools { get; set; }
    public IEnumerable<string> ToolFunctions { get; set; }
    public string TestName { get; set; }
    public string TestName2 { get; set; }
}