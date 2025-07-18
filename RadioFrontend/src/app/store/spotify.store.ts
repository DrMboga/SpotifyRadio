import { SpotifySettings } from '../model/spotify-settings';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { computed, inject } from '@angular/core';
import { BackendService } from '../services/backend.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { SpotifyApiService } from '../services/spotify-api.service';
import { SpotifyDevice } from '../model/spotify-device';
import { PlayListItem } from '../model/spotify-playlist-item';

const SPOTIFY_LOGIN_BASE_URL = 'https://accounts.spotify.com/authorize/';
const SPOTIFY_LOGIN_URL_PARAMETERS =
  '&response_type=code&show_dialog=true&scope=user-read-private user-read-email user-modify-playback-state user-read-playback-position user-library-read streaming user-read-playback-state user-read-recently-played playlist-read-private';

type SpotifyState = {
  settings: SpotifySettings;
  devices: SpotifyDevice[];
  playLists: PlayListItem[];
};

const initialState: SpotifyState = {
  settings: {
    clientId: '',
    clientSecret: '',
    redirectUrl: '',
    authToken: '',
    authTokenExpiration: 0,
    refreshToken: '',
    deviceName: '',
    playlistName: '',
  },
  devices: [],
  playLists: [],
};

export const SpotifyStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withHooks({
    onInit: (store, backend = inject(BackendService)) => {
      backend
        .readSpotifySettings()
        .pipe(takeUntilDestroyed())
        .subscribe(settings => patchState(store, { settings }));
    },
  }),
  withComputed(({ settings }) => ({
    isAuthorized: computed(() => {
      if (
        !settings.clientId?.() ||
        !settings.clientSecret?.() ||
        !settings.redirectUrl?.() ||
        !settings.authToken?.() ||
        !settings.authTokenExpiration?.()
      ) {
        return false;
      }
      const expiration = settings.authTokenExpiration() ?? 0;
      const now = new Date();
      return expiration >= now.getTime();
    }),
    spotifyLoginUrl: computed(() => {
      if (!settings.clientId?.()) {
        return '';
      }
      const redirectUri = `${location.origin}/spotify-login`;
      return `${SPOTIFY_LOGIN_BASE_URL}?client_id=${settings.clientId()}&redirect_uri=${redirectUri}${SPOTIFY_LOGIN_URL_PARAMETERS}`;
    }),
  })),
  withMethods(
    (store, backend = inject(BackendService), spotifyApi = inject(SpotifyApiService)) => ({
      saveSpotifySettings: rxMethod<SpotifySettings>(
        pipe(
          switchMap(settings =>
            backend.saveSpotifySettings(settings).pipe(
              tapResponse({
                next: () => patchState(store, () => ({ settings })),
                error: err => {
                  patchState(store, () => ({ settings: {} }));
                  console.error(err);
                },
              }),
            ),
          ),
        ),
      ),
      getAndSaveAuthenticationToken: rxMethod<{
        code: string;
        clientId: string;
        clientSecret: string;
      }>(
        pipe(
          switchMap(parameters => {
            const redirectUri = `${location.origin}/spotify-login`;
            return spotifyApi
              .getToken(
                parameters.code,
                redirectUri,
                parameters.clientId ?? '',
                parameters.clientSecret ?? '',
              )
              .pipe(
                switchMap(tokenResponse => {
                  return backend
                    .saveSpotifySettings({
                      ...store.settings(),
                      redirectUrl: redirectUri,
                      authToken: tokenResponse.accessToken,
                      authTokenExpiration: tokenResponse.expiration * 1000,
                      refreshToken: tokenResponse.refreshToken,
                    })
                    .pipe(
                      tapResponse({
                        next: () =>
                          patchState(store, () => ({
                            settings: {
                              ...store.settings(),
                              redirectUrl: redirectUri,
                              authToken: tokenResponse.accessToken,
                              authTokenExpiration: tokenResponse.expiration * 1000,
                              refreshToken: tokenResponse.refreshToken,
                            },
                          })),
                        error: err => {
                          patchState(store, () => ({
                            settings: {
                              ...store.settings(),
                              authToken: '',
                              authTokenExpiration: 0,
                              refreshToken: '',
                            },
                          }));
                          console.error(err);
                        },
                      }),
                    );
                }),
              );
          }),
        ),
      ),

      readSpotifyDevices: rxMethod<string>(
        pipe(
          switchMap(authToken =>
            spotifyApi.getDevices(authToken).pipe(
              tapResponse({
                next: devices => patchState(store, () => ({ devices })),
                error: err => {
                  patchState(store, () => ({ devices: [] }));
                  console.error(err);
                },
              }),
            ),
          ),
        ),
      ),

      readPlayLists: rxMethod<string>(
        pipe(
          switchMap(authToken =>
            spotifyApi.getPlayLists(authToken).pipe(
              tapResponse({
                next: playLists => patchState(store, () => ({ playLists })),
                error: err => {
                  patchState(store, () => ({ playLists: [] }));
                  console.error(err);
                },
              }),
            ),
          ),
        ),
      ),
    }),
  ),
);
