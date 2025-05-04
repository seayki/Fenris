# RemoveFenrisFirewallRules.ps1
# Removes all firewall rules with the display name prefix "FenrisBlock_WebBlocker_"

# Ensure the script runs with elevated privileges
$isElevated = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isElevated) {
    Write-Error "This script requires administrative privileges. Please run as Administrator."
    exit 1
}

try {
    # Get all firewall rules with the specified prefix
    $rules = Get-NetFirewallRule | Where-Object { $_.DisplayName -like "FenrisBlock_WebBlocker_*" }

    if ($rules.Count -eq 0) {
        Write-Output "No FenrisBlock firewall rules found."
        exit 0
    }

    # Remove each rule
    foreach ($rule in $rules) {
        Write-Output "Removing firewall rule: $($rule.DisplayName)"
        Remove-NetFirewallRule -DisplayName $rule.DisplayName
    }

    Write-Output "All FenrisBlock firewall rules have been removed successfully."
}
catch {
    Write-Error "Failed to remove firewall rules: $_"
    exit 1
}