import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SpotifySettings } from '../model/spotify-settings';
import { RadioButtonInfo } from '../model/radio-button-info';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';
import { RadioCountry } from '../model/radio-country';

@Injectable({
  providedIn: 'root',
})
export class BackendService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.production ? location.origin : 'http://localhost:5262';

  readSpotifySettings(): Observable<SpotifySettings> {
    const url = `${this.baseUrl}/spotify-settings`;
    return this.http.get<SpotifySettings>(url);
  }

  saveSpotifySettings(settings: SpotifySettings): Observable<SpotifySettings> {
    const url = `${this.baseUrl}/spotify-settings`;
    return this.http.patch<SpotifySettings>(url, settings);
  }

  readRadioButtons(): Observable<RadioButtonInfo[]> {
    const url = `${this.baseUrl}/radio-buttons`;
    return this.http.get<RadioButtonInfo[]>(url);
  }

  getRadioCountries(): Observable<RadioCountry[]> {
    const url = `${this.baseUrl}/radio-countries-list`;
    return this.http.get<RadioCountry[]>(url);
  }

  getSabaChannels(button: number): Observable<RadioChannel[]> {
    const url = `${this.baseUrl}/radio-stations-by-button?button=${button}`;
    return this.http.get<RadioChannel[]>(url);
  }

  setSabaChannel(channel: RadioChannel): Observable<RadioChannel> {
    const url = `${this.baseUrl}/radio-stations-by-button`;
    return this.http.post<RadioChannel>(url, channel);
  }

  deleteSabaChannel(button: number, sabaFrequency: number): Observable<void> {
    const url = `${this.baseUrl}/radio-stations-by-button?button=${button}&sabaFrequency=${sabaFrequency}`;
    return this.http.delete(url).pipe(map(response => void 0));
  }
}
