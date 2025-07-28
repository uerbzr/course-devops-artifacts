# === CONFIGURATION ===
$organization = ""       # Replace with your Azure DevOps org name
$project = ""        # Optional: include if feeds are scoped to a project
$pat = ""               # Replace with your PAT securely

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
