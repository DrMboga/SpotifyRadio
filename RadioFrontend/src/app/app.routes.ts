import { Routes } from '@angular/router';
import { SpotifySettingsComponent } from './spotify-settings/spotify-settings.component';
import { RadioStationsComponent } from './radio-stations/radio-stations.component';

export const routes: Routes = [
  { path: 'spotify', component: SpotifySettingsComponent },
  { path: 'radio', component: RadioStationsComponent },
  { path: '', redirectTo: '/spotify', pathMatch: 'full' },
];
