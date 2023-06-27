using System.Xml.Linq;

namespace NuGetSave.Common;

using NuGetSave.Data;
using NuGetSave.Process;
using static NuGetSave.Process.Enums;

internal static class OptionsBaseExtensions
{
  public static string GetSolutionFileName(this OptionsBase options)
  {
    ArgumentNullException.ThrowIfNull(options, nameof(options));

    if (string.IsNullOrEmpty(options.SolutionFileName))
    {
      var env = Environment.GetEnvironmentVariable("SOLUTION_FILE_NAME");
      if (string.IsNullOrEmpty(env))
      {
        throw new InvalidOperationException("Unable to resolve solution file name.");
      }

      options.SolutionFileName = env;
    }

    return options.SolutionFileName;
  }

  public static string GetProjectFileName(this OptionsBase options)
  {
    ArgumentNullException.ThrowIfNull(options, nameof(options));

    if (string.IsNullOrEmpty(options.ProjectFileName))
    {
      var env = Environment.GetEnvironmentVariable("PROJECT_FILE_NAME");
      if (string.IsNullOrEmpty(env))
      {
        throw new InvalidOperationException("Unable to resolve solution file name.");
      }

      options.ProjectFileName = env;
    }

    return options.ProjectFileName;
  }

  public static async Task<IEnumerable<PackageSource>> GetPackageSourcesAsync(this OptionsBase options, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(options, nameof(options));

    if (!string.IsNullOrEmpty(options.NugetConfigurationFileName))
    {
      if (!File.Exists(options.NugetConfigurationFileName))
      {
        throw new InvalidOperationException($"The nuget configuration file `{options.NugetConfigurationFileName}` does NOT exists.");
      }

      var nugetConfigurationStr = await File.ReadAllTextAsync(options.NugetConfigurationFileName, cancellationToken);
      var nugetConfiguration = XDocument.Parse(nugetConfigurationStr, LoadOptions.PreserveWhitespace);

      XName packageSourcesXName = "packageSources";
      XName collectionXName = "add";

      XName credentialsXName = "packageSourceCredentials";
      XName keyXName = "key";
      XName valueXName = "value";

      if (!nugetConfiguration.Descendants(packageSourcesXName).Any())
      {
        throw new InvalidOperationException($"No nuget package sources were defined in the configuration file `{options.NugetConfigurationFileName}`.");
      }

      if (!nugetConfiguration.Descendants(packageSourcesXName).Descendants(collectionXName).Any())
      {
        throw new InvalidOperationException($"No package sources were found in the configuration file `{options.NugetConfigurationFileName}`.");
      }

      List<PackageSource> packageSources = new();

      foreach (var packageSourceItem in nugetConfiguration.Descendants(packageSourcesXName).Descendants(collectionXName))
      {
        var key = packageSourceItem.Attribute(keyXName)?.Value ?? throw new InvalidOperationException("Invalid name for package source found.");
        var value = packageSourceItem.Attribute(valueXName)?.Value ?? throw new InvalidOperationException("Invalid URL for package source found.");

        PackageSource packageSource = new(key, value);
        XName packageSourceXName = key;

        if (nugetConfiguration.Descendants(credentialsXName).Any() && nugetConfiguration.Descendants(credentialsXName).Descendants(packageSourceXName).Any())
        {
          var credentialItems = nugetConfiguration
            .Descendants(credentialsXName)
            .Descendants(packageSourceXName)
            .Descendants()
            .Where(it => it.Name == collectionXName);

          // we do support only clear text credentials for now
          var userNameNode = credentialItems.SingleOrDefault(it => it.Attribute(keyXName)?.Value == "Username");
          var passwordNode = credentialItems.SingleOrDefault(it => it.Attribute(keyXName)?.Value == "ClearTextPassword");

          packageSource.Credentials = new PackageSourceCredentials()
          {
            UserName = userNameNode?.Attribute("value")?.Value ?? throw new InvalidOperationException("No user name provided."),
            Password = passwordNode?.Attribute("value")?.Value ?? throw new InvalidOperationException("No password provided.")
          };
        }

        packageSources.Add(packageSource);
      }

      return packageSources;
    }

    if (!string.IsNullOrEmpty(options.NugetPackageSourceUrl))
    {
      return new[] { new PackageSource(string.Empty, options.NugetPackageSourceUrl) };
    }

    throw new NotSupportedException("Invalid arguments in provided options (sources).");
  }

  public static LookupTypeEnum GetLookupType(this OptionsBase options)
  {
    ArgumentNullException.ThrowIfNull(options, nameof(options));

    if (string.IsNullOrEmpty(options.SolutionFileName) && string.IsNullOrEmpty(options.ProjectFileName))
    {
      return LookupTypeEnum.None;
    }

    if (!string.IsNullOrEmpty(options.SolutionFileName) && !string.IsNullOrEmpty(options.ProjectFileName))
    {
      return LookupTypeEnum.None;
    }

    if (!string.IsNullOrEmpty(options.SolutionFileName))
    {
      return LookupTypeEnum.Solution;
    }

    if (!string.IsNullOrEmpty(options.ProjectFileName))
    {
      return LookupTypeEnum.Project;
    }

    throw new NotImplementedException("Invalid arguments in provided options (solution/project).");
  }
}
