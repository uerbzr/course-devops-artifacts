# Azure Devops - Artifacts

## Configure devops pipeline to publish a nuget package to a Nuget Artifact Feed

If you want to try with this project, you could start by forking this into your Github, creating a new Azure Devops Project and creating a new project and under Repos importing the code from your Github. You will still need to follow the steps below.

### Create a Personal Access Token

- In the top right hand region, click on User Settings
- Click on Personal Access Tokens
  - Click New Token
  - Write a Name in the textbox
  - Choose suitable expiration period
  - Under Scopes you can grant Full Access.
  - Save this key somewhere outside of the repository so you DO NOT SHARE / PUSH
  - Not necessary for this task but consider looking at a Custom Defined Scope, then show more scopes to see the kind of things you can achieve from a pipeline

### Artifact Setup

- From the main project overview summary page
- Click Create New Feed
- Enter a name into the textbox
- Set the visibility to your required setting
- Check the Upstream sources
- Set the scope radio button
- Click Create

**Once created you should select your feed fromt he dropdown and click Connect to Feed.**

- now click on Nuget.exe and you should see the URL to your feed in the XML under the Project Setup section. Make a note of this!

### Service Connection

To create a Service Connection you should have the feed url and the PAT token from the steps above. Now:

- From the main project page click on the Project Settings in the bottom left corner
- Under 'project settings' go to Service Connections => Pipelines => Service Connections
- Click the New service connection button
- Filter the list to find Nuget, click the radio button to select and Next
- Click on the ApiKey radio button
- Populate Feed Url (Artifact/Nuget feed from earlier)
- Populate the Api Key (PAT from ealier)
- Populate the Service Connection Name
- Check the Grant access permissions to all pipelines
- Click Save

### Pipeline

- Create a file with the following called nuget-artifact-pipeline.yml in the root of the project

```yml
name: "1.0.$(Rev:r)"
trigger:
  - main

pool:
  vmImage: ubuntu-latest

jobs:
  - job: BuildAndTest
    displayName: Build and Test
    steps:
      - task: UseDotNet@2
        displayName: "Install .NET 9 SDK"
        inputs:
          packageType: "sdk"
          version: "9.0.x"
          includePreviewVersions: true
      - task: DotNetCoreCLI@2
        displayName: dotnet restore
        inputs:
          command: "restore"
          projects: "**/workshop.calculator.csproj"
          feedsToUse: "select"
          vstsFeed: "09a3d620-34df-4061-a07f-7f7f531020df"
      - task: DotNetCoreCLI@2
        displayName: dotnet build
        inputs:
          command: "build"
          projects: "**/*.csproj"

      - task: DotNetCoreCLI@2
        displayName: dotnet test
        inputs:
          command: "test"
          projects: "**/*.Tests.csproj"

  - job: CreateNugetPackage
    displayName: Create Nuget Package
    dependsOn: BuildAndTest
    condition: succeeded()
    steps:
      - task: UseDotNet@2
        displayName: "Install .NET 9 SDK"
        inputs:
          packageType: "sdk"
          version: "9.0.x"
          includePreviewVersions: true

      - task: DotNetCoreCLI@2
        displayName: dotnet pack
        inputs:
          command: "pack"
          packagesToPack: "**/workshop.calculator.csproj"
          versioningScheme: "byBuildNumber"

      - task: NuGetAuthenticate@1
        displayName: "Authenticate to Azure Artifacts"

      - task: DotNetCoreCLI@2
        displayName: "Pack NuGet Package"
        inputs:
          command: "pack"
          packagesToPack: "**/*.csproj"
          versioningScheme: "off"
          includesNuGetOrg: true
          arguments: "--configuration Release /p:PackageVersion=$(buildVersion)"

      - task: DotNetCoreCLI@2
        inputs:
          command: "push"
          packagesToPush: "$(Build.ArtifactStagingDirectory)/**/*.nupkg;!$(Build.ArtifactStagingDirectory)/**/*.symbols.nupkg"
          nuGetFeedType: "internal"
          publishVstsFeed: "9c1bc6ac-5cb4-4149-9cad-a096551fb621"
          publishFeedCredentials: "XtonProductionsServiceConnection"

      - task: PublishBuildArtifacts@1
        displayName: publish artifact
        inputs:
          PathtoPublish: "$(Build.ArtifactStagingDirectory)"
          TargetPath: '\\calculator\$(Build.DefinitionName)\$(Build.BuildNumber)'
          ArtifactName: "drop"
          publishLocation: "Container"
```

- Save this and ensure it is pushed
- This won't trigger the pipeline, you have to add it as a new pipeline.
- Click on Pipelines and the Create Pipeline button.
- Click Azure Repos Git YAML option
- Select the Repository
- The pipeline needs to know which project you are building into a nuget package. In this case it'll be workshop.calculator so each of the steps that build will reference this with \*\*/workshop.calculator.csproj .
- Another requirement for the pipeline is to know the GUID of the Artifact that is the Nuget Feed restore and target to push to for example. Use the following powershell with a valid PAT to get the GUIDS.

```powershell
# === CONFIGURATION ===
$organization = "ORG"       # Replace with your Azure DevOps org name
$project = "OPTIONALPROJECT"        # Optional: include if feeds are scoped to a project
$pat = "PAT"               # Replace with your PAT

# === AUTH HEADER ===
$base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$pat"))
$headers = @{ Authorization = "Basic $base64Auth" }

# === API URL ===
$apiVersion = "7.1"
$uri = "https://feeds.dev.azure.com/$organization/_apis/packaging/feeds?api-version=$apiVersion"


# === CALL API ===
$response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers

# === DISPLAY RESULTS ===
$response.value | ForEach-Object {
    Write-Host "Feed Name: $($_.name)"
    Write-Host "Feed GUID: $($_.id)"
    Write-Host "Feed URL: $($_.url)"
    Write-Host "-----------------------------"
}

```

- publishVstsFeed: "9c1bc6ac-5cb4-4149-9cad-a096551fb621"
- publishFeedCredentials: "calculatorserviceserviceconnection"
- once you have the GUID of the feed add it publishVstsFeed of the push section
- add the Service Connection to the publishFeedCredentials entry of the push section
- add a Version to the .csproj file. Everytime you push you should manually increment this. More advanced could be to auto increment this from the pipeline.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Authors>Nigel</Authors>
    <Description>Great Calculator for all your adding needs</Description>
    <Copyright>XtonProductions Code</Copyright>
    <Authors>Nigel</Authors>
    <Version>1.0.100</Version>
    <Company>Xton Productions</Company>
    <PackageId>workshop.calculator</PackageId>
    <Description>Sample calculator logic for NuGet packaging.</Description>
  </PropertyGroup>

</Project>
```

```yml
- task: DotNetCoreCLI@2
  inputs:
    command: "push"
    packagesToPush: "$(Build.ArtifactStagingDirectory)/**/*.nupkg;!$(Build.ArtifactStagingDirectory)/**/*.symbols.nupkg"
    nuGetFeedType: "internal"
    publishVstsFeed: "GUID GOES HERE"
    publishFeedCredentials: "SERVICE CONNECTION GOES HERE"
```

### Useful

In the project that you have consumed the nuget feed, you'll need to do a couple of things.

### Authenticate the URL of the feed

First you'll need to authenticate against the nuget feed you published your package to. In the route of the repo, place a `nuget.config` file with the following contents. Ensure you put the feed name and personal access token.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
<packageSources>
<add key="MyPrivateFeed" value="{urlFeed}" />
</packageSources>
<packageSourceCredentials>
<MyPrivateFeed>
<add key="Username" value="AzureDevOps" />
<add key="ClearTextPassword" value="{PersonalAccessToken}" />
</MyPrivateFeed>
</packageSourceCredentials>
</configuration>
```

Next you should ensure you include a Version for the nuget package. Make sure you manually increment this when you push.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="workshop.calculator" Version="1.0.50" />
  </ItemGroup>

</Project>
```

## Best Practice

An extra step would be to place the variables into a Library Group in the Pipelines section.

- From the main project page go to Pipelines => Library
- Click on the + Variable Group button
- Type a new variable group name into the textbox: ProjectSettings
- Add some variables:

  - Name: projectPath
  - Value: \*\*/workshop.calculator.csproj

- in the pipeline you can simply reference the group at the top:

```yml
variables:
  - group: ProjectSettings
```

- then where needed reference the variable:

```yml
projects: "$(projectPath)"
```

[return](https://github.com/uerbzr/course-devops)
