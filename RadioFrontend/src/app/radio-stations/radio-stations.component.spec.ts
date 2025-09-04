import { ComponentFixture, TestBed } from '@angular/core/testing';
import { expect, jest } from '@jest/globals';
import { RadioStationsComponent } from './radio-stations.component';
import { RadioStore } from '../store/radio.store';
import { RadioButtonInfo } from '../model/radio-button-info';
import { signal } from '@angular/core';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';
import {
  MOCK_RADIO_BUTTONS_LIST,
  MOCK_RADIO_CHANNELS,
  MOCK_RADIO_COUNTRIES,
  MOCK_RADIO_STATION_INFOS,
  MOCK_SABA_CHANNELS_FREQUENCIES,
  MOCK_STATIONS_CACHE_STATUS,
} from '../mock/radio-mock';
import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { RadioCountry } from '../model/radio-country';
import { RadioStationsCacheStatus } from '../model/radio-stations-cache-status';
import { By } from '@angular/platform-browser';

// console.log((fixture.nativeElement as HTMLElement).innerHTML);

describe('RadioStationsComponent', () => {
  let component: RadioStationsComponent;
  let fixture: ComponentFixture<RadioStationsComponent>;
  let radioStore = {
    radioButtonsList: signal<RadioButtonInfo[]>(MOCK_RADIO_BUTTONS_LIST),
    sabaStationsList: signal<number[]>(MOCK_SABA_CHANNELS_FREQUENCIES),
    sabaRadioChannels: signal<RadioChannel[]>(MOCK_RADIO_CHANNELS),
    countries: signal<RadioCountry[]>(MOCK_RADIO_COUNTRIES),
    countryCacheStatus: signal<RadioStationsCacheStatus>(MOCK_STATIONS_CACHE_STATUS),
    countryRadioStations: signal<RadioStationInfo[]>(MOCK_RADIO_STATION_INFOS),
    getSabaRadioChannels: jest.fn(),
    getRadioCountryCacheStatus: jest.fn(),
    setRadioButtonRegion: jest.fn(),
    setSabaRadioChannel: jest.fn(),
    deleteSabaRadioChannel: jest.fn(),
    getRadioStationsByCountry: jest.fn(),
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

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show radio buttons according to SABA buttons list', async () => {
    // Assert
    const matRadioButtons: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('mat-radio-button');
    expect(matRadioButtons.length).toBe(MOCK_RADIO_BUTTONS_LIST.length);

    for (let i = 0; i < MOCK_RADIO_BUTTONS_LIST.length; i++) {
      const label = matRadioButtons[i].querySelector('label');
      expect(label).toBeTruthy();
      expect(label?.textContent?.trim()).toBe(MOCK_RADIO_BUTTONS_LIST[i].buttonLabel);

      const elementAsInput = matRadioButtons[i].querySelector(
        'input[type="radio"]',
      ) as HTMLInputElement;
      expect(elementAsInput).toBeTruthy();

      // First radio button should be disabled
      if (i === 0) {
        expect(elementAsInput.disabled).toBe(true);
      } else {
        expect(elementAsInput.disabled).toBe(false);
      }
    }

    // Second radio button should be selected by default
    const radioGroup = fixture.nativeElement.querySelector('mat-radio-group');
    expect(radioGroup).toBeTruthy();
    expect(radioGroup.getAttribute('ng-reflect-model')).toBe('2');
  });

  it('should show SABA Channels table', async () => {
    // Check if radio channels panel is shown by default, because mock data contains a region associated with default selected radio button
    const radioStationsLeftPanel = fixture.nativeElement.querySelector(
      '.radio-stations-left-panel',
    );
    expect(radioStationsLeftPanel).toBeTruthy();

    const channelsTableRows = Array.from(
      radioStationsLeftPanel.querySelectorAll('tr'),
    ) as HTMLElement[];
    expect(channelsTableRows.length).toBe(MOCK_SABA_CHANNELS_FREQUENCIES.length);

    for (let i = 0; i < MOCK_SABA_CHANNELS_FREQUENCIES.length; i++) {
      const cells = channelsTableRows[i].querySelectorAll('td');
      expect(cells[0].innerHTML.trim()).toBe(`${MOCK_SABA_CHANNELS_FREQUENCIES[i]} MHz`);
    }
  });

  it('should show SABA Radio channel items on an appropriate frequency if data contains a channel', async () => {
    // Check if radio channels panel is shown by default, because mock data contains a region associated with default selected radio button
    const radioStationsLeftPanel = fixture.nativeElement.querySelector(
      '.radio-stations-left-panel',
    );
    expect(radioStationsLeftPanel).toBeTruthy();

    const channelsTableRows = Array.from(
      radioStationsLeftPanel.querySelectorAll('tr'),
    ) as HTMLElement[];
    expect(channelsTableRows.length).toBe(MOCK_SABA_CHANNELS_FREQUENCIES.length);

    for (let i = 0; i < MOCK_SABA_CHANNELS_FREQUENCIES.length; i++) {
      const cells = channelsTableRows[i].querySelectorAll('td');
      const frequency = MOCK_SABA_CHANNELS_FREQUENCIES[i];
      const firstDiv = cells[1].querySelector('div');
      const channelForFrequency = MOCK_RADIO_CHANNELS.find(c => c.sabaFrequency === frequency);
      if (channelForFrequency) {
        // First div should be the radio name
        expect(firstDiv).toBeTruthy();
        expect(firstDiv?.innerHTML).toBe(channelForFrequency.name);
      } else {
        expect(firstDiv).toBeFalsy();
      }
    }
  });

  it('should set countries list to dropdown', async () => {
    // Get the mat-select element
    const matSelect: HTMLElement = fixture.nativeElement.querySelector('mat-select');
    expect(matSelect).toBeTruthy();

    // Click to open the mat-select dropdown
    matSelect.click();
    fixture.detectChanges();

    // Find mat-option elements in the overlay container
    const options = document.querySelectorAll('mat-option');
    expect(options.length).toBe(MOCK_RADIO_COUNTRIES.length);

    const optionTexts = Array.from(options).map(opt => opt.textContent?.trim());
    expect(optionTexts).toEqual(MOCK_RADIO_COUNTRIES.map(c => c.country));
  });

  it('should call cache status method on country select', async () => {
    // Arrange
    const countryIndex = 1; // Netherlands in the mock MOCK_RADIO_COUNTRIES

    const matSelect: HTMLElement = fixture.nativeElement.querySelector('mat-select');
    expect(matSelect).toBeTruthy();
    matSelect.click();
    fixture.detectChanges();

    const options = document.querySelectorAll('mat-option');
    expect(options.length).toBe(MOCK_RADIO_COUNTRIES.length);
    const optionToSelect = Array.from(options)[countryIndex] as HTMLElement;
    expect(optionToSelect).toBeTruthy();

    // Act
    optionToSelect.click();
    fixture.detectChanges();

    // Assert
    expect(component.selectedCountry()!.country).toBe(MOCK_RADIO_COUNTRIES[countryIndex].country);

    // should request cache status on Country select
    expect(radioStore.getRadioCountryCacheStatus).toHaveBeenLastCalledWith(
      MOCK_RADIO_COUNTRIES[countryIndex].country,
    );

    // Check if cache info is shown
    const topRowItems = fixture.debugElement.queryAll(By.css('.top-row-item'));
    expect(topRowItems.length).toBeGreaterThan(2);
    expect(topRowItems[2].nativeElement.textContent.trim()).toBe(
      `${MOCK_STATIONS_CACHE_STATUS.processedCount} radio stations from ${MOCK_STATIONS_CACHE_STATUS.totalStations} cached`,
    );
  });

  // it('should save new SABA Radio channel on drag-drop onto the channels list table on the left', async () => {
  //   const radioStationsRightPanel = fixture.nativeElement.querySelector(
  //     '.radio-stations-right-panel',
  //   );
  //
  //   const radioStationsLeftPanel = fixture.nativeElement.querySelector(
  //     '.radio-stations-left-panel',
  //   );
  //
  //   // Dragging rp[1] -> lp[0],
  //   const radioChannelIndex = 0;
  //   const radioStationIndex = 1;
  //   const radioStation = MOCK_SAARLAND_RADIO_STATIONS[radioStationIndex];
  //   const event = {
  //     previousIndex: radioStationIndex,
  //     currentIndex: radioChannelIndex,
  //     previousContainer: radioStationsRightPanel,
  //     container: radioStationsLeftPanel,
  //     item: {
  //       data: radioStation,
  //     },
  //   };
  //
  //   // Act
  //   component.sabaChannelDrop(event as CdkDragDrop<RadioStationInfo>);
  //
  //   // Assert
  //   expect(radioStore.setSabaRadioChannel).toHaveBeenCalledWith({
  //     sabaFrequency: MOCK_SABA_CHANNELS_FREQUENCIES[radioChannelIndex],
  //     name: radioStation.name,
  //     button: 2,
  //     region: radioStation.region,
  //     streamUrl: radioStation.stationStreamUrl,
  //   });
  //   expect(radioStore.deleteSabaRadioChannel).not.toHaveBeenCalled();
  // });

  it('should remove SABA Radio channel from list on drag-drop onto radio station list on the right', async () => {
    const radioStationsRightPanel = fixture.nativeElement.querySelector(
      '.radio-stations-right-panel',
    );

    const radioStationsLeftPanel = fixture.nativeElement.querySelector(
      '.radio-stations-left-panel',
    );

    // Dragging lp[1] -> rp[0],
    const radioChannelIndex = 1;
    const radioStationIndex = 0;
    const event = {
      previousIndex: radioChannelIndex,
      currentIndex: radioStationIndex,
      previousContainer: radioStationsLeftPanel,
      container: radioStationsRightPanel,
    };

    // Act
    component.radioStationDrop(event as CdkDragDrop<string>);

    // Assert
    expect(radioStore.deleteSabaRadioChannel).toHaveBeenCalledWith({
      button: 2,
      sabaFrequency: MOCK_SABA_CHANNELS_FREQUENCIES[radioChannelIndex],
    });
    expect(radioStore.setSabaRadioChannel).not.toHaveBeenCalled();
  });

  it('should move SABA Radio to another channel on drag-drop onto the same channels list', async () => {
    const radioStationsLeftPanel = fixture.nativeElement.querySelector(
      '.radio-stations-left-panel',
    );

    // Dragging lp[1] -> lp[0],
    const radioChannelCurrentIndex = 0;
    const radioStationPreviousIndex = 1;
    const channelFrequency = MOCK_SABA_CHANNELS_FREQUENCIES[radioStationPreviousIndex];
    const channel = MOCK_RADIO_CHANNELS.find(c => c.sabaFrequency === channelFrequency);
    const event = {
      previousIndex: radioStationPreviousIndex,
      currentIndex: radioChannelCurrentIndex,
      previousContainer: radioStationsLeftPanel,
      container: radioStationsLeftPanel,
      item: {
        data: channel,
      },
    };

    // Act
    component.sabaChannelDrop(event as CdkDragDrop<RadioStationInfo>);

    // Assert
    expect(radioStore.deleteSabaRadioChannel).toHaveBeenCalledWith({
      button: 2,
      sabaFrequency: MOCK_SABA_CHANNELS_FREQUENCIES[radioStationPreviousIndex],
    });
    expect(radioStore.setSabaRadioChannel).toHaveBeenCalledWith({
      sabaFrequency: MOCK_SABA_CHANNELS_FREQUENCIES[radioChannelCurrentIndex],
      name: channel?.name,
      button: 2,
      country: channel?.country,
      radioLogoBase64: channel?.radioLogoBase64,
      stationDetailsUrl: channel?.stationDetailsUrl,
      streamUrl: channel?.streamUrl,
    });
  });
});
