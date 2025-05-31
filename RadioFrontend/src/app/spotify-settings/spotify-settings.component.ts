import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatButton } from '@angular/material/button';
import { SpotifyStore } from '../store/spotify.store';

// import { MatOption, MatSelect } from '@angular/material/select';

@Component({
  selector: 'app-spotify-settings',
  imports: [
    FormsModule,
    MatFormField,
    MatInput,
    MatLabel,
    FormsModule,
    MatButton,
    // MatSelect,
    // MatOption,
  ],
  templateUrl: './spotify-settings.component.html',
  styleUrl: './spotify-settings.component.css',
})
export class SpotifySettingsComponent {
  private readonly spotifyStore = inject(SpotifyStore);

  spotifySettings = this.spotifyStore.settings;
  isAuthorized = this.spotifyStore.isAuthorized;

  public authorize() {
    if (!this.spotifySettings()) {
      return;
    }
    // Save settings before redirect
    this.spotifyStore.saveSpotifySettings(this.spotifySettings());

    // Redirect to Spotify login page
    // const url = `https://accounts.spotify.com/authorize/?client_id=${this.clientId}&response_type=code&redirect_uri=${this.redirectUri}&show_dialog=true&scope=user-read-private user-read-email user-modify-playback-state user-read-playback-position user-library-read streaming user-read-playback-state user-read-recently-played playlist-read-private`;
    // window.location.href = url;
  }
}
