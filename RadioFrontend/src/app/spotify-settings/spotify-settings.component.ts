import { Component, computed, effect, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatButton } from '@angular/material/button';
import { SpotifyStore } from '../store/spotify.store';
import { MatOption, MatSelect } from '@angular/material/select';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-spotify-settings',
  imports: [
    MatFormField,
    MatInput,
    MatLabel,
    FormsModule,
    MatButton,
    MatSelect,
    MatOption,
    MatDividerModule,
  ],
  templateUrl: './spotify-settings.component.html',
  styleUrl: './spotify-settings.component.css',
})
export class SpotifySettingsComponent {
  private readonly spotifyStore = inject(SpotifyStore);

  spotifySettings = this.spotifyStore.settings;
  spotifyDevices = this.spotifyStore.devices;
  spotifyPlaylists = this.spotifyStore.playLists;
  isAuthorized = this.spotifyStore.isAuthorized;

  private authToken = computed(() => {
    if (this.isAuthorized()) {
      return this.spotifyStore.settings().authToken;
    }
    return undefined;
  });

  constructor() {
    effect(() => {
      const authToken = this.authToken();
      if (authToken) {
        this.spotifyStore.readSpotifyDevices(authToken);
        this.spotifyStore.readPlayLists(authToken);
      }
    });
  }

  public authorize() {
    if (!this.spotifySettings()) {
      return;
    }
    // Save settings before redirect
    this.spotifyStore.saveSpotifySettings(this.spotifySettings());

    // Redirect to Spotify login page
    window.location.href = this.spotifyStore.spotifyLoginUrl();
  }

  public saveDeviceAndPlayList(): void {
    if (!this.spotifySettings()) {
      return;
    }
    this.spotifyStore.saveSpotifySettings(this.spotifySettings());
  }
}
