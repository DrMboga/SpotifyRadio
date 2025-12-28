# Raspberry Pi setup

- [Install Raspbian OS](#install-raspberry-pi-imager--os)
-  Configure SSH Keys
    - [Configure SSH Keys Windows](#configure-ssh-key-based-authentication-windows)
    - [Configure SSH Key based authentication (Mac)](#configure-ssh-key-based-authentication-mac)
- [Install system updates](#install-system-updates)
- [Enable UART Serial pot hardware](#enable-uart-serial-pot-hardware)
- [Install Raspotify (spotify connect)](#install-spotify-connect)
- [Install VLC libraries](#vlc-install)
- [install PIGPIO library](#install-pigpio-library)
- [Install .Net SKD](#install-net)
- [InstallPlaywright](#playwright-install-needed-to-run-mytuner-scraper)
- [Depoly main .Net app as service](#run-main-app-as-service)
- [NGINX setup](#setup-nginx)

---

## Install Raspberry Pi imager + OS

Download imager: https://www.raspberrypi.com/software/

Install it, plug in the CD card to computer and walk through the Wizard and don't forget to set up:

- Machine name: `spotifyradio`
- admin user (`pi`) and password;
- WLAN name and password;
- Check `Access via SSH` mark

After installing SD card and switching on the Pi, check if it is available:

```bash
ping spotifyradio
```

---

## Configure SSH Key based authentication (Windows)

### 1. Generate key (Windows machine)

```bash
ssh-keygen -t rsa -b 4096 -C "your_email@example.com"
```

### 2. Copy public key to Raspberry Pi (Windows machine)

```bash
scp -r C:/Users/Mike/.ssh/id_rsa.pub pi@spotifyradio:~/.ssh/authorized_keys
```

### 3. Set permissons to public key (Linux machine)

```bash
chmod 700 ~/.ssh
chmod 400 authorized_keys
```

---

## Configure SSH Key based authentication (Mac)

### 1. Generate an SSH Key Pair on Your Mac

1. Open Terminal on your Mac.
2. Run the following command to generate an SSH key pair:

   ```sh
   ssh-keygen -t rsa -b 4096 -C "your_email@example.com"
   ```

   - `-t rsa`: Specifies the type of key to create, which is RSA.
   - `-b 4096`: Specifies the key size in bits (4096 bits is more secure than the default 2048 bits).
   - `-C "your_email@example.com"`: Adds a comment (usually your email) to the key.

3. When prompted for a file to save the key, press `Enter` to accept the default location (`~/.ssh/id_rsa`).
4. If asked for a passphrase, you can enter one or leave it empty for no passphrase (for convenience, leave it empty, but for security, use a passphrase).

#### 2. Copy the Public Key to Your Raspberry Pi

1. Use `ssh-copy-id` to copy your public key to the Raspberry Pi:

   ```sh
   ssh-copy-id pi@spotifyradio

   ```

2. Enter the password for the `pi` user when prompted. This command will copy the public key to the Raspberry Pi and automatically configure the SSH server to use it.

#### 3. Test SSH Key-Based Login

1. Try logging in to the Raspberry Pi using the key:
   ```sh
   ssh pi@spotifyradio

   ```
   You should be able to log in without being prompted for a password.

---

## Install system updates

```bash
sudo apt update
sudo apt upgrade
```

---

## Enable UART Serial pot hardware

```bash
# 1. 
sudo raspi-config
```
2. Navigate to Interfacing Options → Serial Port.
3. Disable the serial console.
4. Enable the serial port hardware. -->

---

## Install Spotify Connect

### 1. Install Raspotify

```bash

# Install curl and https
sudo apt install -y apt-transport-https curl

# Add the “raspotify” GPG key and its repository.
curl -sSL https://dtcooper.github.io/raspotify/key.asc | sudo tee /usr/share/keyrings/raspotify-archive-keyrings.asc >/dev/null
echo 'deb [signed-by=/usr/share/keyrings/raspotify-archive-keyrings.asc] https://dtcooper.github.io/raspotify raspotify main' | sudo tee /etc/apt/sources.list.d/raspotify.list

# Install raspotify
sudo apt update
sudo apt install raspotify

```

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

---

## VLC install

### Windows

Download VLC for Windows (x64) from VideoLAN:
https://www.videolan.org/vlc/download-windows.html

### Raspberry

```bash

sudo apt update
sudo apt install libvlc-dev vlc

```

---

## Install PIGPIO library

```bash
sudo apt update
sudo apt install -y git build-essential

cd ~
git clone https://github.com/joan2937/pigpio.git
cd pigpio
make
sudo make install
sudo ldconfig
```

---

## Install .Net

https://learn.microsoft.com/en-gb/dotnet/core/install/linux-debian

```bash
wget https://packages.microsoft.com/config/debian/13/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# install the runtume
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-10.0

```

---

## Publish main application

- Create a `~/spotifyRadio` on Raspberry Pi
- On windows mashine make main program and copy it to Raspberry Pi

```bash
# Build frontend
cd RadioFrontend
npm run build

# Build backend
cd ../RadioApp/RadioApp
dotnet publish -c release -r linux-arm64
scp -r ./bin/release/net10.0/linux-arm64/publish/* pi@spotifyradio:/home/pi/spotifyRadio
scp -r ./bin/release/net10.0/linux-arm64/.playwright pi@spotifyradio:/home/pi/spotifyRadio/.playwright
```

### Add run permissions:

```bash
cd ~/spotifyRadio
chmod +x ./RadioApp

# Run app for test:
./RadioApp
```

## Playwright install (needed to run MyTuner scraper)

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
cd ~/spotifyRadio

# make node execute permissions
chmod +x .playwright/node/linux-arm64/node

# install browsers
sudo pwsh playwright.ps1 install

```

---

## Run main app as service

### Copy a service file

On Windows machine:

```bash
scp -r RadioApp/radio-app.service pi@spotifyradio:/home/pi/spotifyRadio/radio-app.service
```

On Raspberry Pi:

```bash
cd ~/spotifyRadio
sudo cp -r ./radio-app.service /etc/systemd/system/radio-app.service
```

### Register a service as `systemctl`
```bash

# Restart daemon
sudo systemctl daemon-reload
# Start services
sudo systemctl start radio-app.service
# Enable auto start
sudo systemctl enable radio-app.service

# check service
sudo journalctl -u radio-app -n 100 --no-pager

```

---


## Setup Nginx

```bash
sudo apt install nginx
sudo systemctl start nginx
```

### Permissions to web folder:

```bash
sudo gpasswd -a www-data pi

chmod g+x /home/pi && chmod g+x /home/pi/spotifyRadio && chmod g+x /home/pi/spotifyRadio/wwwroot && chmod g+x /home/pi/spotifyRadio/wwwroot/browser && chmod g+x /home/pi/spotifyRadio/wwwroot/browser
```

### Create a self-signed certificate

```bash
sudo mkdir -p /etc/ssl/localcerts
cd /etc/ssl/localcerts
sudo openssl req -x509 -nodes -days 365 \
  -newkey rsa:2048 \
  -keyout dotnet-api.key \
  -out dotnet-api.crt

```

You’ll be prompted for info; the important one is:

Common Name (CN):
Use : `spotifyradio.local`


You’ll get:

`/etc/ssl/localcerts/dotnet-api.crt`
`/etc/ssl/localcerts/dotnet-api.key`

- Set permissions:

```bash
sudo chmod 600 /etc/ssl/localcerts/dotnet-api.key
```

### Nginx config
```bash
sudo nano /etc/nginx/nginx.conf
```

```json
# Redirect all HTTP to HTTPS
server {
    listen 80;
    server_name _;

    return 301 https://$host$request_uri;
}

# HTTPS reverse proxy to Kestrel
server {
    listen 443 ssl;
    server_name spotifyradio.local;

    ssl_certificate     /etc/ssl/localcerts/dotnet-api.crt;
    ssl_certificate_key /etc/ssl/localcerts/dotnet-api.key;

    # (Optional) hardening
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_prefer_server_ciphers on;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}

```

```bash
sudo nginx -t
sudo systemctl reload nginx
```

## Access web site

https://spotifyradio.local

---
