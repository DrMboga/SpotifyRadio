import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RadioStationsComponent } from './radio-stations.component';
import { RadioStore } from '../store/radio.store';
import { RadioButtonInfo } from '../model/radio-button-info';
import { signal } from '@angular/core';
import { RadioButtonRegion } from '../model/radio-button-region';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';

describe('RadioStationsComponent', () => {
  let component: RadioStationsComponent;
  let fixture: ComponentFixture<RadioStationsComponent>;
  let radioStore = {
    radioButtonsList: signal<RadioButtonInfo[]>([]),
    regionsList: signal<string[]>([]),
    radioButtonRegions: signal<RadioButtonRegion[]>([]),
    sabaStationsList: signal<string[]>([]),
    regionStationsList: signal<RadioStationInfo[]>([]),
    sabaRadioChannels: signal<RadioChannel[]>([]),
    getSabaRadioChannels: jest.fn(),
    getRadioStationsByRegion: jest.fn(),
    setRadioButtonRegion: jest.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RadioStationsComponent],
      providers: [{ provide: RadioStore, useValue: radioStore }],
    }).compileComponents();

    fixture = TestBed.createComponent(RadioStationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
