namespace NuGetSave.Data;

internal class PackageSource
{
  public string Name { get; } = string.Empty;

  public string Url { get; } = string.Empty;

  public PackageSourceCredentials? Credentials { get; set; }

  public PackageSource(string name, string url)
  {
    Name = name ?? throw new ArgumentNullException(nameof(name));
    Url = url ?? throw new ArgumentNullException(nameof(url));
  }
}
