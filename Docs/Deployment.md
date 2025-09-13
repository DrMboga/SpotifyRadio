# Raspberry Pi setup

## Raspotify

### Install

[TBD]

### Setup

1. Get the device name:

```bash
aplay -l
```

For 3.5mm output it should be something like: 
`card 0: Headphones [bcm2835 Headphones], device 0: bcm2835 Headphones [bcm2835 Headphones]`

So, we need then ALSA card 0, device 0: `plughw:0,0`

2. Config
```bash
sudo nano /etc/raspotify/conf
```

```ini

LIBRESPOT_NAME=SABA

# Audio backend and output
LIBRESPOT_BACKEND=alsa
LIBRESPOT_DEVICE=plughw:0,0       # <-- set from `aplay -l`
LIBRESPOT_BITRATE=160


```

```bash
sudo systemctl restart raspotify
```

3. Diagnostic:

```bash
sudo journalctl -u raspotify -n 100 --no-pager
```

### Making librespot working

If logs shows thefollowing error:

` ERROR librespot_connect::spirc] starting dealer failed: Invalid state { Websocket couldn't be started because: Deadline expired before operation could complete { Connection timed out (os error 110) } }`

then try these 2 commands:

```bash
# IPv6 test (likely to hang or timeout on your setup)
curl -6 -I https://dealer.spotify.com/  -m 8

# IPv4 fallback test (should connect fast and show HTTP headers)
curl -4 -I https://dealer.spotify.com/  -m 8
```

If IPv6 hangs but IPv4 connects quickly, Try to setup prefer IPv4 over IPv6:

- This one helps temporary:

```bash
sudo sysctl -w net.ipv6.conf.all.disable_ipv6=1
```

- To make it permanent (haven't tried yet):

```bash
echo "net.ipv6.conf.all.disable_ipv6=1" | sudo tee /etc/sysctl.d/99-disable-ipv6.conf
sudo sysctl -p /etc/sysctl.d/99-disable-ipv6.conf
sudo reboot
```

More Info: https://chatgpt.com/g/g-p-68790e87dcdc81918ca5cd2ccfdadd85-radio/c/68c550c9-ec88-832e-b01d-f84f3bcb1fb1


## VLC install

### Windows

Download VLC for Windows (x64) from VideoLAN:
https://www.videolan.org/vlc/download-windows.html

### Raspberry

```bash

sudo apt update
sudo apt install libvlc-dev vlc

```


## Playwright install

1. Install Powershell 

Go to this page https://github.com/PowerShell/PowerShell/releases
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
