import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SpotifySettings } from '../model/spotify-settings';
import { RadioButtonInfo } from '../model/radio-button-info';
import { RadioButtonRegion } from '../model/radio-button-region';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';

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

  readRadioRegions(): Observable<string[]> {
    const url = `${this.baseUrl}/radio-regions`;
    return this.http.get<string[]>(url);
  }

  readRadioButtons(): Observable<RadioButtonInfo[]> {
    const url = `${this.baseUrl}/radio-buttons`;
    return this.http.get<RadioButtonInfo[]>(url);
  }

  readRadioButtonRegions(): Observable<RadioButtonRegion[]> {
    const url = `${this.baseUrl}/radio-button-region`;
    return this.http.get<RadioButtonRegion[]>(url);
  }
  setRadioButtonRegion(buttonRegion: RadioButtonRegion): Observable<RadioButtonRegion> {
    const url = `${this.baseUrl}/radio-button-region`;
    return this.http.post<RadioButtonRegion>(url, buttonRegion);
  }

  getRadioStationsByRegion(region: string): Observable<RadioStationInfo[]> {
    const url = `${this.baseUrl}/radio-stations-by-region?region=${region}`;
    return this.http.get<RadioStationInfo[]>(url);
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
