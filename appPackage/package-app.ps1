# PowerShell script to create the Teams app package
# This script creates a .zip file that can be uploaded to Microsoft Teams

param(
    [string]$OutputPath = "TeamsAssistBot.zip"
)

Write-Host "Creating Teams app package..." -ForegroundColor Green

# Check if required files exist
$requiredFiles = @("manifest.json", "color.png", "outline.png")
$missingFiles = @()

foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host "Missing required files:" -ForegroundColor Red
    foreach ($file in $missingFiles) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Please ensure all required files are present:" -ForegroundColor Yellow
    Write-Host "  - manifest.json (Teams app manifest)" -ForegroundColor Yellow
    Write-Host "  - color.png (192x192 app icon)" -ForegroundColor Yellow
    Write-Host "  - outline.png (32x32 outline icon)" -ForegroundColor Yellow
    exit 1
}

# Create the zip package
try {
    # Remove existing package if it exists
    if (Test-Path $OutputPath) {
        Remove-Item $OutputPath -Force
        Write-Host "Removed existing package: $OutputPath" -ForegroundColor Yellow
    }

    # Create new zip package
    Compress-Archive -Path $requiredFiles -DestinationPath $OutputPath -Force
    
    Write-Host "Teams app package created successfully: $OutputPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Upload this package to Microsoft Teams Admin Center or" -ForegroundColor White
    Write-Host "2. Use 'Upload a custom app' in Teams client" -ForegroundColor White
    Write-Host "3. Search for your organization's apps and install 'Jarvis'" -ForegroundColor White
    
} catch {
    Write-Host "Error creating package: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
