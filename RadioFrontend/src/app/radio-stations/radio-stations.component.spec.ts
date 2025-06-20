import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { RadioStationsComponent } from './radio-stations.component';
import { RadioStore } from '../store/radio.store';
import { RadioButtonInfo } from '../model/radio-button-info';
import { signal } from '@angular/core';
import { RadioButtonRegion } from '../model/radio-button-region';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';
import { MOCK_RADIO_BUTTONS_LIST, MOCK_RADIO_REGIONS_LIST } from '../mock/radio-mock';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatRadioButtonHarness } from '@angular/material/radio/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { MatOptionHarness } from '@angular/material/core/testing';

describe('RadioStationsComponent', () => {
  let component: RadioStationsComponent;
  let fixture: ComponentFixture<RadioStationsComponent>;
  let radioStore = {
    radioButtonsList: signal<RadioButtonInfo[]>(MOCK_RADIO_BUTTONS_LIST),
    regionsList: signal<string[]>(MOCK_RADIO_REGIONS_LIST),
    radioButtonRegions: signal<RadioButtonRegion[]>([]),
    sabaStationsList: signal<string[]>([]),
    regionStationsList: signal<RadioStationInfo[]>([]),
    sabaRadioChannels: signal<RadioChannel[]>([]),
    getSabaRadioChannels: jest.fn(),
    getRadioStationsByRegion: jest.fn(),
    setRadioButtonRegion: jest.fn(),
  };

  let harnessLoader: HarnessLoader;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [RadioStationsComponent],
      providers: [{ provide: RadioStore, useValue: radioStore }],
    })
      .compileComponents()
      .then(async () => {
        fixture = TestBed.createComponent(RadioStationsComponent);
        component = fixture.componentInstance;

        harnessLoader = TestbedHarnessEnvironment.loader(fixture);
        fixture.detectChanges();
      });
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show radio buttons according to SABA buttons list', async () => {
    // Assert
    const radioButtons = await harnessLoader.getAllHarnesses(MatRadioButtonHarness);

    expect(radioButtons).toBeTruthy();
    expect(radioButtons.length).toBe(MOCK_RADIO_BUTTONS_LIST.length);

    for (let i = 0; i < MOCK_RADIO_BUTTONS_LIST.length; i++) {
      expect(await radioButtons[i].getLabelText()).toBe(MOCK_RADIO_BUTTONS_LIST[i].buttonLabel);
    }

    // First radio button should be disabled
    expect(radioButtons[0].isDisabled()).toBeTruthy();

    // Second radio button should be selected by default
    expect(radioButtons[1].getValue).toBeTruthy();
  });

  it('should set regions list to dropdown', async () => {
    // Assert
    const matSelect = await harnessLoader.getHarness(MatSelectHarness);
    // await (await matSelect.host()).click();
    // await matSelect.open();

    expect(matSelect).toBeTruthy();
    const options = await matSelect.getOptions();
    // expect(options.length).toBe(MOCK_RADIO_REGIONS_LIST.length);
  });

  // should request appropriate region for selected button and set up the dropdown if exists

  // should request SABA radio channels list on radio Button click

  // should leave regions dropdown value empty and hide radio stations table if no region for button setup

  // should set SaveRegion button disabled if no new region selected in dropdown

  // should set SaveRegion button enabled when new region for button selected

  // should call appropriate method on SaveRegion button click

  // should request stations info list on Region select

  // should show radio stations list items

  // should show SABA Channels table

  // should show SABA Radio channel items on an appropriate frequency if data contains a channel

  // should disable radio station info list item on the left if it is present in the SABA Channels list on the right

  // should save new SABA Radio channel on drag-drop onto the channels list table on the left

  // should remove SABA Radio channel from list on drag-drop onto radio station list on the right
});
