using Microsoft.Extensions.Logging;

namespace NuGetSave.Protocol;

// TODO:
internal class NugetProtocolLogger : NuGet.Common.ILogger
{
  private readonly Microsoft.Extensions.Logging.ILogger logger;

  public NugetProtocolLogger(Microsoft.Extensions.Logging.ILogger logger)
  {
    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public void Log(NuGet.Common.LogLevel level, string data)
  {
  }

  public void Log(NuGet.Common.ILogMessage message)
  {
  }

  public Task LogAsync(NuGet.Common.LogLevel level, string data)
  {
    return Task.CompletedTask;
  }

  public Task LogAsync(NuGet.Common.ILogMessage message)
  {
    return Task.CompletedTask;
  }

  public void LogDebug(string data)
  {
    logger.LogDebug(message: data);
  }

  public void LogError(string data)
  {
    logger.LogError(message: data);
  }

  public void LogInformation(string data)
  {
    logger.LogInformation(message: data);
  }

  public void LogInformationSummary(string data)
  {
    logger.LogInformation(message: data);
  }

  public void LogMinimal(string data)
  {
    logger.LogTrace(message: data);
  }

  public void LogVerbose(string data)
  {
    logger.LogTrace(message: data);
  }

  public void LogWarning(string data)
  {
    logger.LogWarning(message: data);
  }
}
