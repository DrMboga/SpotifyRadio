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

type SpotifyState = {
  settings: SpotifySettings;
};

const initialState: SpotifyState = {
  settings: {},
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
