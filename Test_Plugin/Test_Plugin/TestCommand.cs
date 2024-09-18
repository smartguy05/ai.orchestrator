using System;
using System.Threading.Tasks;
using Ai.Orchestrator.Models.Interfaces;
using Ai.Orchestrator.Common.Extensions;

namespace Test_Plugin;

public class TestCommand : ICommand
{
    public string Name => "TEST";
    public string Description  => "Displays hello message.";

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
    
    public async Task<object> Execute(object request, string configString)
    {
        var args = request.GetServiceRequest<TestClass>();
        var config = configString.ReadConfig<TestConfig>();

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

        return Task.FromResult(Task.FromResult((object)0));
    }
}

public class TestClass
{
    public string Name { get; set; }
    public int Value { get; set; }
}

public class TestConfig: IPluginConfig
{
    public object Contract { get; set; }
    public string Description { get; set; }
    public string TestName { get; set; }
    public string TestName2 { get; set; }
}