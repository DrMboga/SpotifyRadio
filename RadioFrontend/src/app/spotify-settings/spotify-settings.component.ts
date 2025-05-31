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
    window.location.href = this.spotifyStore.spotifyLoginUrl();
  }
}
