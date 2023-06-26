using CommandLine;

namespace NuGetSave.Process.DownloadPackages;

[Verb("download", isDefault: false)]
internal class DownloadPackagesOptions : OptionsBase
{
  [Option('d', "target-directory", Required = true)]
  public string TargetDirectory { get; set; } = string.Empty;

  [Option("include-prerelease", Required = false, Default = false)]
  public bool IncludePrerelease { get; set; }

  [Option("include-unlisted", Required = false, Default = false)]
  public bool IncludeUnlisted { get; set; }
}
