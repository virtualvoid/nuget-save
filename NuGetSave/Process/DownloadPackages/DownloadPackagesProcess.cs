using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetSave.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetSave.Process.DownloadPackages;

internal class DownloadPackagesProcess : ProcessBase
{
  private readonly DownloadPackagesOptions options;

  public DownloadPackagesProcess(DownloadPackagesOptions options, ILogger<DownloadPackagesProcess> logger)
    : base(options, logger)
  {
    this.options = options ?? throw new ArgumentNullException(nameof(options));
  }

  public override async Task ProcessAsync(CancellationToken cancellationToken)
    => await ExecuteAsync(
      async (packageSourceInfoes, referenceInfoes, cancellationToken) =>
      {
        var nugetProtocolLogger = new NugetProtocolLogger(logger);

        Console.WriteLine($"Creating directory: {options.TargetDirectory}");
        Directory.CreateDirectory(options.TargetDirectory);

        foreach(var packageSourceInfo in packageSourceInfoes)
        {
          SourceCacheContext cacheContext = new();

          Console.WriteLine($"Constructing package source: {packageSourceInfo.Name}");
          PackageSource packageSource = new(packageSourceInfo.Url, packageSourceInfo.Name);
          if (packageSourceInfo.Credentials != null)
          {
            packageSource.Credentials = new PackageSourceCredential(
              packageSource.Source,
              packageSourceInfo.Credentials.UserName,
              packageSourceInfo.Credentials.Password,
              true,
              null
            );
          }

          SourceRepository repository = Repository.Factory.GetCoreV3(packageSource);
          PackageMetadataResource metadataResource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

          foreach(var referenceInfo in referenceInfoes)
          {
            Console.WriteLine($"Looking for: {referenceInfo.Name}");

            var metadatas = await metadataResource.GetMetadataAsync(
              referenceInfo.Name, options.IncludePrerelease, options.IncludeUnlisted, cacheContext, nugetProtocolLogger, cancellationToken
            );

            Console.WriteLine($"- Got {metadatas.Count()} result(s)...");
           

            if (NuGetVersion.TryParse(referenceInfo.Version, out var referenceVersion))
            { // can parse, download that exact version

            }
            else
            { // unable to parse, download latest version

            }
          }
        }
      },
      cancellationToken
    );
}
