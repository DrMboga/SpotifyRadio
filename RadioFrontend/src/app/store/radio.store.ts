import { RadioButtonInfo } from '../model/radio-button-info';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { inject } from '@angular/core';
import { BackendService } from '../services/backend.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RadioButtonRegion } from '../model/radio-button-region';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';

const SABA_MIN_FREQUENCY = 87;
const SABA_MAX_FREQUENCY = 105;

type RadioSettingsState = {
  regionsList: string[];
  radioButtonsList: RadioButtonInfo[];
  radioButtonRegions: RadioButtonRegion[];
  sabaStationsList: number[]; //87-105 MHz
  regionStationsList: RadioStationInfo[];
  sabaRadioChannels: RadioChannel[];
};

const initialState: RadioSettingsState = {
  regionsList: [],
  radioButtonsList: [],
  radioButtonRegions: [],
  sabaStationsList: [],
  regionStationsList: [],
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
        .readRadioRegions()
        .pipe(takeUntilDestroyed())
        .subscribe(regionsList => {
          patchState(store, { regionsList });
        });
      backend
        .readRadioButtons()
        .pipe(takeUntilDestroyed())
        .subscribe(radioButtonsList => {
          patchState(store, { radioButtonsList });
        });
      backend
        .readRadioButtonRegions()
        .pipe(takeUntilDestroyed())
        .subscribe(radioButtonRegions => {
          patchState(store, { radioButtonRegions });
        });
    },
  }),
  withMethods((store, backend = inject(BackendService)) => ({
    setRadioButtonRegion: rxMethod<{ sabaRadioButton: number; region: string }>(
      pipe(
        switchMap(({ sabaRadioButton, region }) =>
          backend.setRadioButtonRegion({ sabaRadioButton, region }).pipe(
            tapResponse({
              next: radioButtonRegion => {
                patchState(store, () => ({
                  radioButtonRegions: [
                    ...store
                      .radioButtonRegions()
                      .filter(
                        region => region.sabaRadioButton !== radioButtonRegion.sabaRadioButton,
                      ),
                    radioButtonRegion,
                  ],
                }));
              },
              error: err => {
                console.error(err);
              },
            }),
          ),
        ),
      ),
    ),
    getRadioStationsByRegion: rxMethod<string>(
      pipe(
        switchMap(region =>
          backend.getRadioStationsByRegion(region).pipe(
            tapResponse({
              next: regionStationsList => patchState(store, () => ({ regionStationsList })),
              error: err => {
                patchState(store, () => ({ regionStationsList: [] }));
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
  })),
);
