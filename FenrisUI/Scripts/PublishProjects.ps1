# Set the paths to your projects
$fenrisUIProjectPath = "C:\Users\dudis\source\repos\Fenris\FenrisUI\FenrisUI.csproj"
$fenrisServiceProjectPath = "C:\Users\dudis\source\repos\Fenris\WindowsMonitorService\WindowsMonitorService.csproj"
# Publish FenrisUI
Write-Host "Publishing FenrisUI..."
dotnet publish $fenrisUIProjectPath -c Release -r win-x64
Write-Host "FenrisUI published."

# Publish FenrisService
Write-Host "Publishing WindowsMonitorService..."
dotnet publish $fenrisServiceProjectPath -c Release -r win-x64
Write-Host "FenrisService published."

Write-Host "Build and publish process complete."