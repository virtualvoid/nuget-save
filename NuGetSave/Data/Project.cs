namespace NuGetSave.Data;

internal class Project
{
  public Guid Id { get; set; }

  public Guid ParentId { get; set; }

  public string Name { get; set; } = string.Empty;

  public string RelativePath { get; set; } = string.Empty;

  public string FullPath { get; set; } = string.Empty;
}
