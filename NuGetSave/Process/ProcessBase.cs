using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NuGetSave.Process;

using NuGetSave.Common;
using NuGetSave.Data;
using static NuGetSave.Process.Enums;

internal abstract class ProcessBase : IDisposable
{
  // credits: https://stackoverflow.com/a/26129175
  private static readonly Regex projectRx = new("Project\\(\"(?<ParentProjectGuid>{[A-F0-9-]+})\"\\) = \"(?<ProjectName>.*?)\", \"(?<RelativePath>.*?)\", \"(?<ProjectGuid>{[A-F0-9-]+})");

  private readonly OptionsBase options;

  protected readonly ILogger logger;

  protected ProcessBase(OptionsBase options, ILogger logger)
  {
    this.options = options ?? throw new ArgumentNullException(nameof(options));
    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public virtual void Dispose()
  {
  }

  public abstract Task ProcessAsync(CancellationToken cancellationToken);

  protected virtual async Task ExecuteAsync(Func<IEnumerable<PackageSource>, IEnumerable<ProjectReference>, CancellationToken, Task> func, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(func, nameof(func));

    Console.WriteLine("Loading package sources ...");
    IEnumerable<PackageSource> packageSources = await options.GetPackageSourcesAsync(cancellationToken);

    IEnumerable<ProjectReference> references = options.GetLookupType() switch
    {
      LookupTypeEnum.Solution => await LoadSolutionReferencesAsync(options.GetSolutionFileName(), null, cancellationToken),
      LookupTypeEnum.Project => await LoadProjectReferencesAsync(options.GetProjectFileName(), null, cancellationToken),

      _ => throw new NotSupportedException($"The lookup type `{options.GetLookupType()}` is NOT supported."),
    };

    await func(packageSources, references, cancellationToken);
  }

  private async Task<IEnumerable<ProjectReference>> LoadSolutionReferencesAsync(string solutionFileName, IList<ProjectReference>? references, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(solutionFileName, nameof(solutionFileName));

    var localSolutionPath = Path.GetDirectoryName(solutionFileName) ?? throw new InvalidOperationException($"Unable to obtain local project path from `{solutionFileName}`.");
    Console.WriteLine($"Loading solution file: {Path.GetFileName(solutionFileName)}");

    List<Project> projects = new();
    foreach (var solutionLine in await File.ReadAllLinesAsync(solutionFileName, cancellationToken))
    {
      var match = projectRx.Match(solutionLine);
      if (!match.Success)
      {
        continue;
      }

      var parentId = Guid.Parse(match.Groups[1].Value);
      var name = match.Groups[2].Value;
      var relativePath = match.Groups[3].Value;
      var id = Guid.Parse(match.Groups[4].Value);

      // only add reachable projects (ignore solution folders, etc.)
      var projectFileName = Path.Combine(localSolutionPath, relativePath);
      if (!File.Exists(projectFileName))
      {
        continue;
      }

      Console.WriteLine($"- {name}");

      projects.Add(
        new()
        {
          Id = id,
          ParentId = parentId,
          Name = name,
          RelativePath = relativePath,
          FullPath = projectFileName
        }
      );
    }

    references ??= new List<ProjectReference>();
    foreach (var project in projects)
    {
      Console.WriteLine($"Loading references from: {project.Name}");

      _ = await LoadProjectReferencesAsync(project.FullPath, references, cancellationToken);
    }

    Console.WriteLine();

    return references;
  }

  private async Task<IEnumerable<ProjectReference>> LoadProjectReferencesAsync(string projectFileName, IList<ProjectReference>? references, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(projectFileName, nameof(projectFileName));

    references ??= new List<ProjectReference>();

    var localProjectPath = Path.GetDirectoryName(projectFileName) ?? throw new InvalidOperationException($"Unable to obtain local project path from `{projectFileName}`.");
    var localProjectFileName = Path.GetFileName(projectFileName);

    var projectDocumentStr = await File.ReadAllTextAsync(projectFileName, cancellationToken);
    var projectDocument = XDocument.Parse(projectDocumentStr, LoadOptions.PreserveWhitespace);

    // process direct references first
    XName packageReferenceXName = "PackageReference";
    XName includeXName = "Include";
    XName versionXName = "Version";

    foreach (var packageReferenceItem in projectDocument.Descendants(packageReferenceXName))
    {
      var name = packageReferenceItem.Attribute(includeXName)?.Value;
      if (string.IsNullOrEmpty(name))
      {
        logger.LogWarning($"Unable to find element `{includeXName}` in package references: {projectDocumentStr}");
        continue;
      }

      var version = packageReferenceItem.Attribute(versionXName)?.Value ?? string.Empty;
      if (string.IsNullOrEmpty(name))
      {
        logger.LogWarning($"Unable to find element `{versionXName}` in package references, will use latest.");
      }

      if (references.Any(it => it.Name == name && it.Version == version))
      {
        logger.LogDebug($"The reference `{name}` version `{version}` is already added, therefore skipping.");
        continue;
      }

      ProjectReference projectReference = new(name, version)
      {
        ProjectFileName = localProjectFileName
      };

      references.Add(projectReference);
    }

    // process project references
    XName projectReferenceXName = "ProjectReference";

    foreach (var projectReferenceItem in projectDocument.Descendants(projectReferenceXName))
    {
      var name = projectReferenceItem.Attribute(includeXName)?.Value;
      if (string.IsNullOrEmpty(name))
      {
        logger.LogWarning($"Unable to find element `{includeXName}` in project references: {projectDocumentStr}");
        continue;
      }

      await LoadProjectReferencesAsync(Path.Combine(localProjectPath, name), references, cancellationToken);
    }

    return references;
  }
}
