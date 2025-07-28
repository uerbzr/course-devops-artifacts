# Azure Devops - Artifacts

## Configure devops pipeline to publish a nuget package to a Nuget Artifact Feed

To get a nuget feed pipeline up and running work through the steps below:

### Personal Access Token

- In the top right hand region, click on User Settings
- Click on Personal Access Tokens
  - Click New Token
  - Write a Name in the textbox
  - Choose suitable expiration period
  - Select permissions - Packaging, Build, Release should be given significant access
  - Save this key somewhere outside of the repository so you DO NOT SHARE
  - Not necessary for this task but consider looking at Show more scopes to see the kind of things you can achieve from a pipeline

### Artifact Setup

- From the main project page click the Articacts button
- Click Create New Feed
- Enter a name into the textbox
- Set the visibility to your required setting
- Check the Upstream sources
- Set the scope radio button
- Click Create

Once created you should select your feed fromt he dropdown and click Connect to Feed.

- now click on Nuget.exe and you should see the path to your feed in the XML under the Project Setup section.

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

- The pipeline needs to know which project you are building into a nuget package. In this case it'll be workshop.calculator so each of the steps that build will reference this with \*\*/workshop.calculator.csproj .
- Another requirement for the pipeline is to know the GUID of the Artifact that is the Nuget Feed. Few tricks to obtain this including the below powershell script to

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

- once you have the GUID of the feed add it to line 73 of the pipeline
- add the Service Connection on line 74

```yml
- task: DotNetCoreCLI@2
  inputs:
    command: "push"
    packagesToPush: "$(Build.ArtifactStagingDirectory)/**/*.nupkg;!$(Build.ArtifactStagingDirectory)/**/*.symbols.nupkg"
    nuGetFeedType: "internal"
    publishVstsFeed: "GUID GOES HERE"
    publishFeedCredentials: "SERVICE CONNECTION GOES HERE"
```
