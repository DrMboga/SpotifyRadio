import { jest } from '@jest/globals';
import { of, Subject } from 'rxjs';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { BackendService } from '../services/backend.service';
import { RadioStore } from './radio.store';
import {
  MOCK_RADIO_BUTTON_REGIONS,
  MOCK_RADIO_BUTTONS_LIST,
  MOCK_RADIO_CHANNELS,
  MOCK_RADIO_REGIONS_LIST,
  MOCK_SAARLAND_RADIO_STATIONS,
} from '../mock/radio-mock';
import { RadioButtonRegion } from '../model/radio-button-region';
import { RadioChannel } from '../model/radio-channel';

describe('RadioStore', () => {
  let store: any;
  let backend = {
    readRadioRegions: jest.fn().mockReturnValue(of(MOCK_RADIO_REGIONS_LIST)),
    readRadioButtons: jest.fn().mockReturnValue(of(MOCK_RADIO_BUTTONS_LIST)),
    readRadioButtonRegions: jest.fn().mockReturnValue(of(MOCK_RADIO_BUTTON_REGIONS)),
    setRadioButtonRegion: jest
      .fn()
      // @ts-ignore
      .mockImplementation((buttonRegion: RadioButtonRegion) => of(buttonRegion)),
    getRadioStationsByRegion: jest.fn().mockReturnValue(of(MOCK_SAARLAND_RADIO_STATIONS)),
    getSabaChannels: jest.fn().mockReturnValue(of(MOCK_RADIO_CHANNELS)),
    // @ts-ignore
    setSabaChannel: jest.fn().mockImplementation((channel: RadioChannel) => of(channel)),
    deleteSabaChannel: jest.fn().mockReturnValue(of(void 0)),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: BackendService, useValue: backend }],
    });

    store = TestBed.inject(RadioStore);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should read default properties from backend on init', () => {
    expect(backend.readRadioRegions).toHaveBeenCalled();
    expect(backend.readRadioButtons).toHaveBeenCalled();
    expect(backend.readRadioButtonRegions).toHaveBeenCalled();

    const expectedChannels = Array.from({ length: 105 - 87 + 1 }, (_, i) => i + 87);
    expect(store.sabaStationsList()).toEqual(expectedChannels);
    expect(store.regionsList()).toEqual(MOCK_RADIO_REGIONS_LIST);
    expect(store.radioButtonsList()).toEqual(MOCK_RADIO_BUTTONS_LIST);
    expect(store.radioButtonRegions()).toEqual(MOCK_RADIO_BUTTON_REGIONS);
  });

  it('should reset radio button region', fakeAsync(() => {
    const newRadioButtonRegionSubject$ = new Subject<{ sabaRadioButton: number; region: string }>();
    store.setRadioButtonRegion(newRadioButtonRegionSubject$);
    const newButton = 3;
    const newRegion = 'fakeRegion';

    newRadioButtonRegionSubject$.next({ sabaRadioButton: newButton, region: newRegion });
    tick(100);

    // Assert
    expect(backend.setRadioButtonRegion).toHaveBeenCalledWith({
      sabaRadioButton: newButton,
      region: newRegion,
    });
    expect(store.radioButtonRegions()).toEqual([
      MOCK_RADIO_BUTTON_REGIONS[0],
      { ...MOCK_RADIO_BUTTON_REGIONS[1], region: newRegion },
    ]);
  }));

  it('should get getRadioStationsByRegion', fakeAsync(() => {
    const regionToGetSubject$ = new Subject<string>();
    const newRegion = 'fakeRegion';

    store.getRadioStationsByRegion(regionToGetSubject$);

    // Act
    regionToGetSubject$.next(newRegion);
    tick(100);

    // Assert
    expect(backend.getRadioStationsByRegion).toHaveBeenCalledWith(newRegion);
    expect(store.regionStationsList()).toEqual(MOCK_SAARLAND_RADIO_STATIONS);
  }));

  it('should get getSabaRadioChannels', fakeAsync(() => {
    const buttonSubject$ = new Subject<number>();
    const newButton = 4;
    store.getSabaRadioChannels(buttonSubject$);

    // Act
    buttonSubject$.next(newButton);
    tick(100);

    // Assert
    expect(backend.getSabaChannels).toHaveBeenCalledWith(newButton);
    expect(store.sabaRadioChannels()).toEqual(MOCK_RADIO_CHANNELS);
  }));

  it('should save SabaRadioChannel', fakeAsync(() => {
    const newRadioChannelSubject$ = new Subject<RadioChannel>();
    const newRadioChannel: RadioChannel = {
      button: 4,
      name: 'mockChannel',
      region: 'mockRegion',
      sabaFrequency: 103,
    };

    store.setSabaRadioChannel(newRadioChannelSubject$);

    // Act
    newRadioChannelSubject$.next(newRadioChannel);
    tick(100);

    // Assert
    expect(backend.setSabaChannel).toHaveBeenCalledWith(newRadioChannel);
    expect(store.sabaRadioChannels()).toEqual([newRadioChannel]);
  }));

  it('should delete SABA radio channel', fakeAsync(() => {
    const channelToDeleteSubject$ = new Subject<{ button: number; sabaFrequency: number }>();
    const button = 4;
    const sabaFrequency = 103;
    store.deleteSabaRadioChannel(channelToDeleteSubject$);

    // Act
    channelToDeleteSubject$.next({ button, sabaFrequency });
    tick(100);

    // Assert
    expect(backend.deleteSabaChannel).toHaveBeenCalledWith(button, sabaFrequency);
  }));
});
