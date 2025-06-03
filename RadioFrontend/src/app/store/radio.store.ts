import { RadioButtonInfo } from '../model/radio-button-info';
import { patchState, signalStore, withHooks, withMethods, withState } from '@ngrx/signals';
import { inject } from '@angular/core';
import { BackendService } from '../services/backend.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RadioButtonRegion } from '../model/radio-button-region';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';

type RadioSettingsState = {
  regionsList: string[];
  radioButtonsList: RadioButtonInfo[];
  radioButtonRegions: RadioButtonRegion[];
};

const initialState: RadioSettingsState = {
  regionsList: [],
  radioButtonsList: [],
  radioButtonRegions: [],
};

export const RadioStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withHooks({
    onInit: (store, backend = inject(BackendService)) => {
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
  })),
);
