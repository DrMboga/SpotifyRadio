import { RadioButtonInfo } from '../model/radio-button-info';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { inject } from '@angular/core';
import { BackendService } from '../services/backend.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { RadioChannel } from '../model/radio-channel';
import { RadioCountry } from '../model/radio-country';
import { RadioStationsCacheStatus } from '../model/radio-stations-cache-status';
import { RadioStationInfo } from '../model/radio-station-info';

const SABA_MIN_FREQUENCY = 87;
const SABA_MAX_FREQUENCY = 105;

type RadioSettingsState = {
  radioButtonsList: RadioButtonInfo[];
  countries: RadioCountry[];
  countryCacheStatus: RadioStationsCacheStatus;
  countryRadioStations: RadioStationInfo[];
  sabaStationsList: number[]; //87-105 MHz
  sabaRadioChannels: RadioChannel[];
};

const initialState: RadioSettingsState = {
  radioButtonsList: [],
  countries: [],
  countryCacheStatus: { totalStations: 0, processedCount: 0 },
  countryRadioStations: [],
  sabaStationsList: [],
  sabaRadioChannels: [],
};

export const RadioStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withHooks({
    onInit: (store, backend = inject(BackendService)) => {
      const sabaStationsList = Array.from(
        { length: SABA_MAX_FREQUENCY - SABA_MIN_FREQUENCY + 1 },
        (_, i) => i + SABA_MIN_FREQUENCY,
      );
      patchState(store, { sabaStationsList });
      backend
        .readRadioButtons()
        .pipe(takeUntilDestroyed())
        .subscribe(radioButtonsList => {
          patchState(store, { radioButtonsList });
        });
      backend
        .getRadioCountries()
        .pipe(takeUntilDestroyed())
        .subscribe(countries => {
          patchState(store, { countries });
        });
    },
  }),
  withMethods((store, backend = inject(BackendService)) => ({
    getRadioCountryCacheStatus: rxMethod<string>(
      pipe(
        switchMap(country =>
          backend.getRadioStationsCacheStatus(country).pipe(
            tapResponse({
              next: countryCacheStatus => patchState(store, { countryCacheStatus }),
              error: err => {
                patchState(store, () => ({
                  countryCacheStatus: { totalStations: 0, processedCount: 0 },
                }));
                console.error(err);
              },
            }),
          ),
        ),
      ),
    ),
    getSabaRadioChannels: rxMethod<number>(
      pipe(
        switchMap(button =>
          backend.getSabaChannels(button).pipe(
            tapResponse({
              next: sabaRadioChannels => patchState(store, () => ({ sabaRadioChannels })),
              error: err => {
                patchState(store, () => ({ sabaRadioChannels: [] }));
                console.error(err);
              },
            }),
          ),
        ),
      ),
    ),
    setSabaRadioChannel: rxMethod<RadioChannel>(
      pipe(
        switchMap(channel =>
          backend.setSabaChannel(channel).pipe(
            tapResponse({
              next: channel =>
                patchState(store, () => ({
                  sabaRadioChannels: [
                    ...store
                      .sabaRadioChannels()
                      .filter(c => c.sabaFrequency !== channel.sabaFrequency),
                    channel,
                  ],
                })),
              error: err => {
                console.error(err);
              },
            }),
          ),
        ),
      ),
    ),
    deleteSabaRadioChannel: rxMethod<{ button: number; sabaFrequency: number }>(
      pipe(
        switchMap(({ button, sabaFrequency }) =>
          backend.deleteSabaChannel(button, sabaFrequency).pipe(
            tapResponse({
              next: () =>
                patchState(store, () => ({
                  sabaRadioChannels: [
                    ...store.sabaRadioChannels().filter(c => c.sabaFrequency !== sabaFrequency),
                  ],
                })),
              error: err => {
                console.error(err);
              },
            }),
          ),
        ),
      ),
    ),
    startCacheRadioStations: rxMethod<{ country: string; countryUrl: string }>(
      pipe(
        switchMap(({ country, countryUrl }) =>
          backend.startCacheRadioStations(country, countryUrl),
        ),
      ),
    ),
    getRadioStationsByCountry: rxMethod<string>(
      pipe(
        switchMap(country =>
          backend.getRadioStationsByCountry(country).pipe(
            tapResponse({
              next: countryRadioStations => patchState(store, () => ({ countryRadioStations })),
              error: err => {
                patchState(store, () => ({ countryRadioStations: [] }));
                console.error(err);
              },
            }),
          ),
        ),
      ),
    ),
    clearCache: rxMethod<void>(
      pipe(
        switchMap(() =>
          backend.clearCache().pipe(
            tapResponse({
              next: () =>
                patchState(store, () => ({
                  countries: [],
                  countryRadioStations: [],
                })),
              error: err => {
                console.error(err);
              },
            }),
          ),
        ),
      ),
    ),
  })),
);
