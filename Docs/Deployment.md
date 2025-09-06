# Raspberry Pi setup

## Playwright install

1. Install Powershell 

Go to this page https://github.com/PowerShell/PowerShell/releases/tag/v7.5.2
And choose the `linux-arm64.tar.gz` version of powershell distrib. For example:
`powershell-7.5.2-linux-arm64.tar.gz`

```bash
# Update the list of packages
sudo apt-get update
sudo apt-get install -y jq libssl1.1 libunwind8

# Install pre-requisite packages.
sudo apt-get install -y wget

# Download the PowerShell package file
wget https://github.com/PowerShell/PowerShell/releases/download/v7.5.2/powershell-7.5.2-linux-arm64.tar.gz

###################################
# Extract to a directory:
mkdir -p ~/powershell
tar -xvf powershell-7.5.2-linux-arm64.tar.gz -C ~/powershell

# Set up permissions
chmod +x ~/powershell/pwsh
chmod u+rx ~/powershell

# Create a symbolic link for easy access:
sudo ln -s ~/powershell/pwsh /usr/bin/pwsh

# Delete the downloaded package file
rm powershell-7.5.2-linux-arm64.tar.gz

# Start PowerShell
pwsh

```

2. Install playwright required browsers.

```bash
cd RadioApp
# Build solution
sudo dotnet build

# install browsers
sudo pwsh RadioApp/bin/Debug/net9.0/playwright.ps1 install

```
