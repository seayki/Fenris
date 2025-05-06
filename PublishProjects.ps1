# Set the paths to your projects
$fenrisUIProjectPath = "C:\Users\dudis\source\repos\Fenris\FenrisUI\FenrisUI.csproj"
$fenrisServiceProjectPath = "C:\Users\dudis\source\repos\Fenris\FenrisService\FenrisService.csproj"

# Publish FenrisUI
Write-Host "Publishing FenrisUI..."
dotnet publish $fenrisUIProjectPath -c Release -r win-x64 --self-contained true
Write-Host "FenrisUI published."

# Publish FenrisService
Write-Host "Publishing FenrisService..."
dotnet publish $fenrisServiceProjectPath -c Release -r win-x64 --self-contained true
Write-Host "FenrisService published."

Write-Host "Build and publish process complete."