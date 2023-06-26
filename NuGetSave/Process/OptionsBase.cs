using CommandLine;

namespace NuGetSave.Process;

internal class OptionsBase
{
  [Option('s', "solution", Required = false)]
  public string? SolutionFileName { get; set; }

  [Option('p', "project", Required = false)]
  public string? ProjectFileName { get; set; }

  [Option('c', "nuget-configuration", Required = false)]
  public string? NugetConfigurationFileName { get; set; }

  [Option('u', "nuget-package-source", Required = false)]
  public string? NugetPackageSourceUrl { get; set; }
}
