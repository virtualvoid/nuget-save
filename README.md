# nuget-save

Downloads & extracts the NuGet packages.

**Usage:**

- To list packages contained in project (using nuget.config) :
	`
	NuGetSave.exe list -p|--project BouncyBudgie.Console.csproj -c|--nuget-configuration nuget.config
	`
- To list packages contained in project (using just NuGet API endpoint) :
	`
	NuGetSave.exe list -p|--project BouncyBudgie.Console.csproj -u|--nuget-package-source https://api.nuget.org/v3/index.json
	`
- To list packages contained in solution (using nuget.config) :
	`
	NuGetSave.exe list -s|--solution BouncyBudgie.sln -c|--nuget-configuration nuget.config
	`
- To list packages contained in solution (using just NuGet API endpoint) :
	`
	NuGetSave.exe list -s|--solution BouncyBudgie.sln -u|--nuget-package-source https://api.nuget.org/v3/index.json
	`

- To download packages contained in project (using nuget.config) :
	`
	NuGetSave.exe download -p|--project BouncyBudgie.Console.csproj -c|--nuget-configuration nuget.config -d|--target-directory d:\temp
	`
- To download packages contained in project (using just NuGet API endpoint) :
	`
	NuGetSave.exe download -p|--project BouncyBudgie.Console.csproj -u|--nuget-package-source https://api.nuget.org/v3/index.json -d|--target-directory d:\temp
	`
- To download packages contained in solution (using nuget.config) :
	`
	NuGetSave.exe download -s|--solution BouncyBudgie.sln -c|--nuget-configuration nuget.config -d|--target-directory d:\temp
	`
- To download packages contained in solution (using just NuGet API endpoint) :
	`
	NuGetSave.exe download -s|--solution BouncyBudgie.sln -u|--nuget-package-source https://api.nuget.org/v3/index.json -d|--target-directory d:\temp
	`


The `download` verb also have these two (self-explanatory) flags :
 - --include-prerelease 
 - --include-unlisted

Both of them are `false` when not used.