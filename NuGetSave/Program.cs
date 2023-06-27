// INFO: https://learn.microsoft.com/en-us/nuget/reference/nuget-client-sdk
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace NuGetSave;

using NuGetSave.Process;

internal class Program
{
  private static readonly CancellationTokenSource cancellationTokenSource = new();

  static Program()
  {
    Console.CancelKeyPress += (sender, e) => cancellationTokenSource.Cancel(true);
  }

  static async Task Main(string[] args)
  {
    var x = @"
                        _                       
                       | |                      
 _ __  _   _  __ _  ___| |_ ___  __ ___   _____ 
| '_ \| | | |/ _` |/ _ \ __/ __|/ _` \ \ / / _ \
| | | | |_| | (_| |  __/ |_\__ \ (_| |\ V /  __/
|_| |_|\__,_|\__, |\___|\__|___/\__,_| \_/ \___|
              __/ |                             
             |___/                              
                                version 1.0
";

    Console.WriteLine(x);
    Console.WriteLine();

    var environmentName = Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT") ?? "Development";
    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    var fileProvider = new PhysicalFileProvider(baseDirectory);

    var configuration = new ConfigurationBuilder()
      .SetBasePath(baseDirectory)
      .SetFileProvider(fileProvider)
      .AddJsonFile($"appsettings.json", false)
      .AddJsonFile($"appsettings.{environmentName}.json", true)
      .AddEnvironmentVariables()
      .Build();

    var loggerFactory = LoggerFactory.Create(
      loggingBuilder =>
      {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddConfiguration(configuration);

        loggingBuilder.AddDebug();
      }
    );

    var logger = loggerFactory.CreateLogger<Program>();

    var commandLine = Parser.Default.ParseArguments(args,
      typeof(Process.DownloadPackages.DownloadPackagesOptions),
      typeof(Process.ListPackages.ListPackagesOptions)
    );

    if (commandLine.Errors.Any())
    {
      return;
    }

    var sw = Stopwatch.StartNew();
    try
    {
      Console.WriteLine("Starting new job");
      Console.WriteLine();

      using ProcessBase process = commandLine.Value switch
      {
        Process.DownloadPackages.DownloadPackagesOptions options => new Process.DownloadPackages.DownloadPackagesProcess(options, loggerFactory.CreateLogger<Process.DownloadPackages.DownloadPackagesProcess>()),
        Process.ListPackages.ListPackagesOptions options => new Process.ListPackages.ListPackagesProcess(options, loggerFactory.CreateLogger<Process.ListPackages.ListPackagesProcess>()),

        _ => throw new NotSupportedException("Unsupported arguments were provided.")
      };

      await process.ProcessAsync(cancellationTokenSource.Token);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Error occurred: {ex.Message}");

      cancellationTokenSource.Cancel();
    }
    finally
    {
      sw.Stop();

      Console.WriteLine($"Finished in: {sw.Elapsed}");
    }
  }
}