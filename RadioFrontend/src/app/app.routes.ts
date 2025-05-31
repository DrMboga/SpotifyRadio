import { Routes } from '@angular/router';
import { SpotifySettingsComponent } from './spotify-settings/spotify-settings.component';
import { RadioStationsComponent } from './radio-stations/radio-stations.component';
import { SpotifyLoginCodeComponent } from './spotify-login-code/spotify-login-code.component';

export const routes: Routes = [
  { path: 'spotify', component: SpotifySettingsComponent },
  { path: 'spotify-login', component: SpotifyLoginCodeComponent },
  { path: 'radio', component: RadioStationsComponent },
  { path: '', redirectTo: '/spotify', pathMatch: 'full' },
];
