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

const SPOTIFY_LOGIN_BASE_URL = 'https://accounts.spotify.com/authorize/';
const SPOTIFY_LOGIN_URL_PARAMETERS =
  '&response_type=code&show_dialog=true&scope=user-read-private user-read-email user-modify-playback-state user-read-playback-position user-library-read streaming user-read-playback-state user-read-recently-played playlist-read-private';

type SpotifyState = {
  settings: SpotifySettings;
};

const initialState: SpotifyState = {
  settings: {
    clientId: '',
    clientSecret: '',
    redirectUrl: '',
    authToken: '',
    authTokenExpiration: new Date(1),
    refreshToken: '',
    deviceName: '',
    playlistName: '',
  },
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
        !settings.clientId ||
        !settings.clientId() ||
        !settings.clientSecret ||
        !settings.clientSecret() ||
        !settings.redirectUrl ||
        !settings.redirectUrl() ||
        !settings.authToken ||
        !settings.authToken() ||
        !settings.authTokenExpiration ||
        !settings.authTokenExpiration()
      ) {
        return false;
      }
      const expiration = settings.authTokenExpiration();
      const now = new Date();
      return expiration!.getTime() >= now.getTime();
    }),
    spotifyLoginUrl: computed(() => {
      if (!settings.clientId || !settings.clientId()) {
        return '';
      }
      const redirectUri = `${location.origin}/spotify-login`;
      return `${SPOTIFY_LOGIN_BASE_URL}?client_id=${settings.clientId()}&redirect_uri=${redirectUri}${SPOTIFY_LOGIN_URL_PARAMETERS}`;
    }),
  })),
  withMethods((store, backend = inject(BackendService)) => ({
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
  })),
);
