import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SpotifySettings } from '../model/spotify-settings';
import { RadioButtonInfo } from '../model/radio-button-info';
import { RadioButtonRegion } from '../model/radio-button-region';

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

  readRadioRegions():Observable<string[]>{
    const url = `${this.baseUrl}/radio-regions`;
    return this.http.get<string[]>(url);
  }

  readRadioButtons():Observable<RadioButtonInfo[]>{
    const url = `${this.baseUrl}/radio-buttons`;
    return this.http.get<RadioButtonInfo[]>(url);
  }

  readRadioButtonRegions():Observable<RadioButtonRegion[]>{
    const url = `${this.baseUrl}/radio-button-region`;
    return this.http.get<RadioButtonRegion[]>(url);
  }
  setRadioButtonRegion(buttonRegion: RadioButtonRegion):Observable<RadioButtonRegion>{
    const url = `${this.baseUrl}/radio-button-region`;
    return this.http.post<RadioButtonRegion>(url, buttonRegion);
  }
}
