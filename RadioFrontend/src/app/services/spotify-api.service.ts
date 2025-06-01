import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, of } from 'rxjs';
import { currentDateInSeconds } from '../helpers/date-helper';
import { SpotifyDevice } from '../model/spotify-device';
import { PlayListItem } from '../model/spotify-playlist-item';
import { PlayRequest } from '../model/spotify-play-request';
import { SongInfo } from '../model/spotify-song-info';

const SPOTIFY_BASE_ADDRESS = 'https://accounts.spotify.com/api';
const TOKEN_ADDRESS = 'token';
const DEVICES = 'https://api.spotify.com/v1/me/player/devices';
const PLAYLISTS = 'https://api.spotify.com/v1/me/playlists';

const SHUFFLE = 'https://api.spotify.com/v1/me/player/shuffle';
const PLAY = 'https://api.spotify.com/v1/me/player/play';
const PAUSE = 'https://api.spotify.com/v1/me/player/pause';
const NEXT = 'https://api.spotify.com/v1/me/player/next';
const PREVIOUS = 'https://api.spotify.com/v1/me/player/previous';
const PLAYER = 'https://api.spotify.com/v1/me/player';
const CURRENTLY_PLAYING = 'https://api.spotify.com/v1/me/player/currently-playing';

@Injectable({
  providedIn: 'root',
})
export class SpotifyApiService {
  private readonly http = inject(HttpClient);

  public getToken(
    code: string,
    redirectUri: string,
    clientId: string,
    clientSecret: string,
  ): Observable<{ accessToken: string; expiration: number; refreshToken: string }> {
    if (!redirectUri || !clientId || !clientSecret) {
      throw new Error('Redirect URI or client id or client secret is not set');
    }
    const formData = new URLSearchParams();
    formData.set('grant_type', 'authorization_code');
    formData.set('code', code);
    formData.set('redirect_uri', redirectUri);

    const basicAuth = btoa(`${clientId}:${clientSecret}`);

    const headers = {
      'content-type': 'application/x-www-form-urlencoded',
      Authorization: `Basic ${basicAuth}`,
    };

    return this.http.post(`${SPOTIFY_BASE_ADDRESS}/${TOKEN_ADDRESS}`, formData, { headers }).pipe(
      map((data: any) => {
        const currentDate = currentDateInSeconds();
        const expires: number = +data.expires_in;
        return {
          accessToken: data.access_token.toString(),
          expiration: currentDate + expires,
          refreshToken: data.refresh_token.toString(),
        };
      }),
    );
  }

  public refreshToken(
    refreshToken: string,
    clientId: string,
    clientSecret: string,
  ): Observable<{ accessToken: string; expiration: number; refreshToken: string }> {
    const formData = new URLSearchParams();
    formData.set('grant_type', 'refresh_token');
    formData.set('refresh_token', refreshToken);

    const basicAuth = btoa(`${clientId}:${clientSecret}`);

    const headers = {
      'content-type': 'application/x-www-form-urlencoded',
      Authorization: `Basic ${basicAuth}`,
    };

    return this.http.post(`${SPOTIFY_BASE_ADDRESS}/${TOKEN_ADDRESS}`, formData, { headers }).pipe(
      map((data: any) => {
        const currentDate = currentDateInSeconds();
        const expires: number = +data.expires_in;
        return {
          accessToken: data.access_token.toString(),
          expiration: currentDate + expires,
          refreshToken: refreshToken,
        };
      }),
    );
  }

  public getDevices(token?: string): Observable<SpotifyDevice[]> {
    if (!token) {
      return of([]);
    }
    const headers = {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    };

    return this.http
      .get<{ devices: SpotifyDevice[] }>(DEVICES, { headers })
      .pipe(map(response => response.devices));
  }

  public getPlayLists(token?: string): Observable<PlayListItem[]> {
    if (!token) {
      return of([]);
    }
    const headers = {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    };

    return this.http
      .get<{ items: PlayListItem[] }>(PLAYLISTS, { headers })
      .pipe(map(data => data?.items ?? []));
  }

  public play(deviceId: string, request: PlayRequest, token?: string): Observable<any> {
    if (!token) {
      return of(undefined);
    }

    const headers = {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    };

    return this.http.put(`${PLAY}?device_id=${deviceId}`, request, { headers });
  }

  public getCurrentlyPlayingInfo(token?: string): Observable<SongInfo | null> {
    if (!token) {
      return of(null);
    }

    const headers = {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    };

    return this.http.get<SongInfo | null>(`${CURRENTLY_PLAYING}?market=DE`, { headers });
  }
}
