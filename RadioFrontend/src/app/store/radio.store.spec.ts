import { jest } from '@jest/globals';
import { of, Subject } from 'rxjs';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { BackendService } from '../services/backend.service';
import { RadioStore } from './radio.store';
import {
  MOCK_RADIO_BUTTONS_LIST,
  MOCK_RADIO_CHANNELS,
  MOCK_RADIO_COUNTRIES,
} from '../mock/radio-mock';
import { RadioChannel } from '../model/radio-channel';
import { signal } from '@angular/core';
import { RadioCountry } from '../model/radio-country';

describe('RadioStore', () => {
  let store: any;
  let backend = {
    readRadioButtons: jest.fn().mockReturnValue(of(MOCK_RADIO_BUTTONS_LIST)),
    getSabaChannels: jest.fn().mockReturnValue(of(MOCK_RADIO_CHANNELS)),
    getRadioCountries: jest.fn().mockReturnValue(of(MOCK_RADIO_COUNTRIES)),
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
    expect(backend.readRadioButtons).toHaveBeenCalled();

    const expectedChannels = Array.from({ length: 105 - 87 + 1 }, (_, i) => i + 87);
    expect(store.sabaStationsList()).toEqual(expectedChannels);
    expect(store.radioButtonsList()).toEqual(MOCK_RADIO_BUTTONS_LIST);
    expect(store.countries()).toEqual(MOCK_RADIO_COUNTRIES);
  });

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
