import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SpotifySettings } from '../model/spotify-settings';

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
}
