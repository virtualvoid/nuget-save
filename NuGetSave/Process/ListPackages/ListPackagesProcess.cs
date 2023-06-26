using Microsoft.Extensions.Logging;

namespace NuGetSave.Process.ListPackages;

internal class ListPackagesProcess : ProcessBase
{
  public ListPackagesProcess(ListPackagesOptions options, ILogger<ListPackagesProcess> logger)
    : base(options, logger)
  {
  }

  public override async Task ProcessAsync(CancellationToken cancellationToken)
    => await ExecuteAsync(
      (packageSourceInfoes, referenceInfoes, cancellationToken) =>
      {
        Console.WriteLine("Package Sources :");
        foreach (var packageSourceInfo in packageSourceInfoes)
        {
          Console.WriteLine($"{packageSourceInfo.Name}: {packageSourceInfo.Url}, authenticated: {(packageSourceInfo.Credentials != null)}");
        }

        Console.WriteLine();
        Console.WriteLine("References :");
        foreach (var referenceInfo in referenceInfoes)
        {
          Console.WriteLine($"{referenceInfo.Name}: {referenceInfo.Version}");
        }

        return Task.CompletedTask;
      },
      cancellationToken
    );
}
