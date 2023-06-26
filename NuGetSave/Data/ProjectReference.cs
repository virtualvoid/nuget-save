namespace NuGetSave.Data;

internal class ProjectReference : IEquatable<ProjectReference>
{
  public string Name { get; } = string.Empty;

  public string Version { get; } = string.Empty;

  public string ProjectFileName { get; set; } = string.Empty;

  public ProjectReference(string name, string version)
  {
    Name = name ?? throw new ArgumentNullException(nameof(name));
    Version = version ?? throw new ArgumentNullException(nameof(version));
  }

  public bool Equals(ProjectReference? other)
  {
    return other != null && other.Name == Name && other.Version == Version;
  }

  public override bool Equals(object? obj)
  {
    return Equals(obj as ProjectReference);
  }

  public override int GetHashCode()
  {
    HashCode hash = new();

    hash.Add(Name);
    hash.Add(Version);

    return hash.ToHashCode();
  }
}
