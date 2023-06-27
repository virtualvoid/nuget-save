using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetSave.Process.DownloadPackages;

using NuGet.Packaging;
using NuGetSave.Protocol;

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

        foreach (var packageSourceInfo in packageSourceInfoes)
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
          FindPackageByIdResource finderResource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

          foreach (var referenceInfo in referenceInfoes)
          {
            Console.Write($"Looking for: {referenceInfo.Name}");

            var metadatas = await metadataResource.GetMetadataAsync(
              referenceInfo.Name, options.IncludePrerelease, options.IncludeUnlisted, cacheContext, nugetProtocolLogger, cancellationToken
            );

            Console.Write($"{string.Empty}{metadatas.Count()} result(s):");

            IPackageSearchMetadata? metadata = null;

            if (NuGetVersion.TryParse(referenceInfo.Version, out var referenceVersion))
            { // can parse, download that exact version
              metadata = metadatas.SingleOrDefault(it => it.Identity.Version == referenceVersion);
            }
            else
            { // unable to parse, download latest version that at least partially matches
              // this happens from time to time with packages like `System.ServiceModel.Duplex` (4.10.*)
              // as i described here: https://github.com/eliasby/nusave/issues/20
              // any other issue with versioning should be handled here ->
              logger.LogWarning($"Package: {referenceInfo.Name} has invalid version: {referenceInfo.Version}");

              var referenceVersionFixed = referenceInfo.Version.Replace(".*", string.Empty);
              foreach (var foundMetadata in metadatas.OrderByDescending(it => it.Identity.Version))
              {
                var foundMetadataVersion = foundMetadata.Identity.Version.ToFullString();
                if (foundMetadataVersion.StartsWith(referenceVersionFixed))
                {
                  logger.LogWarning($"Resolved to: {foundMetadataVersion}");

                  referenceVersion = foundMetadata.Identity.Version;
                  metadata = foundMetadata;
                  break;
                }
              }
            }

            if (metadata == null)
            {
              Console.WriteLine($"Unable to locate the metadata for version `{referenceInfo.Version}` !");
              continue;
            }

            Console.WriteLine($"{string.Empty}Downloading: {referenceInfo.Name}{referenceInfo.Version}");

            using var packageMemoryStream = new MemoryStream();

            var success = await finderResource.CopyNupkgToStreamAsync(
              metadata.Identity.Id,
              referenceVersion,
              packageMemoryStream,
              cacheContext,
              nugetProtocolLogger,
              cancellationToken
            );

            var packagePath = Path.Combine(options.TargetDirectory, $"{metadata.Identity.Id}.{referenceVersion.ToFullString()}");
            Directory.CreateDirectory(packagePath);

            using var packageReader = new PackageArchiveReader(packageMemoryStream);
            await packageReader.CopyFilesAsync(
              packagePath,
              packageReader.GetFiles(),
              (sourceFileName, targetFileName, sourceFileStream) =>
              {
                var error = false;
                try
                {
                  Console.Write($"- Extracting: {sourceFileName}...");

                  var targetDirectoryName = Path.GetDirectoryName(targetFileName)
                    ?? throw new InvalidOperationException($"Unable to obtain path from `{targetFileName}`.");

                  Directory.CreateDirectory(targetDirectoryName);

                  using var targetFileStream = new FileStream(targetFileName, FileMode.Create);
                  sourceFileStream.CopyTo(targetFileStream);

                  return targetFileName;
                }
                catch (Exception ex)
                {
                  error = true;

                  logger.LogError(ex, $"Error occurred while extracting: {sourceFileName} from {referenceInfo.Name}");

                  throw;
                }
                finally
                {
                  Console.WriteLine(error ? "FAIL" : "OK");
                }
              },
              nugetProtocolLogger,
              cancellationToken
            );
          }
        }
      },
      cancellationToken
    );
}
