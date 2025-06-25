import { ComponentFixture, TestBed } from '@angular/core/testing';
import { expect, jest } from '@jest/globals';
import { RadioStationsComponent } from './radio-stations.component';
import { RadioStore } from '../store/radio.store';
import { RadioButtonInfo } from '../model/radio-button-info';
import { signal } from '@angular/core';
import { RadioButtonRegion } from '../model/radio-button-region';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';
import {
  MOCK_RADIO_BUTTON_REGIONS,
  MOCK_RADIO_BUTTONS_LIST,
  MOCK_RADIO_CHANNELS,
  MOCK_RADIO_REGIONS_LIST,
  MOCK_SAARLAND_RADIO_STATIONS,
  MOCK_SABA_CHANNELS_FREQUENCIES,
} from '../mock/radio-mock';
import { CdkDragDrop } from '@angular/cdk/drag-drop';

describe('RadioStationsComponent', () => {
  let component: RadioStationsComponent;
  let fixture: ComponentFixture<RadioStationsComponent>;
  let radioStore = {
    radioButtonsList: signal<RadioButtonInfo[]>(MOCK_RADIO_BUTTONS_LIST),
    regionsList: signal<string[]>(MOCK_RADIO_REGIONS_LIST),
    radioButtonRegions: signal<RadioButtonRegion[]>(MOCK_RADIO_BUTTON_REGIONS),
    sabaStationsList: signal<number[]>(MOCK_SABA_CHANNELS_FREQUENCIES),
    regionStationsList: signal<RadioStationInfo[]>(MOCK_SAARLAND_RADIO_STATIONS),
    sabaRadioChannels: signal<RadioChannel[]>(MOCK_RADIO_CHANNELS),
    getSabaRadioChannels: jest.fn(),
    getRadioStationsByRegion: jest.fn(),
    setRadioButtonRegion: jest.fn(),
    setSabaRadioChannel: jest.fn(),
    deleteSabaRadioChannel: jest.fn(),
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

  it('should request appropriate region for selected button and set up the dropdown if exists', async () => {
    // Second radio button should be selected by default
    const radioGroup = fixture.nativeElement.querySelector('mat-radio-group');
    expect(radioGroup).toBeTruthy();
    expect(radioGroup.getAttribute('ng-reflect-model')).toBe('2');

    expect(component.selectedButton()).toBe(2);
    const currentButtonRegion = component.currentButtonRegion();
    expect(currentButtonRegion).toBeTruthy();
    expect(currentButtonRegion?.sabaRadioButton).toBe(MOCK_RADIO_BUTTON_REGIONS[0].sabaRadioButton);
    expect(currentButtonRegion?.region).toBe(MOCK_RADIO_BUTTON_REGIONS[0].region);

    // Check if channels requested
    expect(radioStore.getSabaRadioChannels).toHaveBeenCalledWith(2);

    // Check if SetRegion button is disabled
    const button = fixture.nativeElement.querySelector('button');
    expect(button).toBeTruthy();
    expect(button?.disabled).toBe(true);
  });

  it('should request SABA radio channels list and change Region on radio Button click', async () => {
    const matRadioButtons: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('mat-radio-button');
    const elementAsInput = matRadioButtons[2].querySelector(
      'input[type="radio"]',
    ) as HTMLInputElement;

    // Act
    elementAsInput.click();
    fixture.detectChanges();

    // Assert
    expect(component.selectedButton()).toBe(3);
    const currentButtonRegion = component.currentButtonRegion();
    expect(currentButtonRegion).toBeTruthy();
    expect(currentButtonRegion?.sabaRadioButton).toBe(MOCK_RADIO_BUTTON_REGIONS[1].sabaRadioButton);
    expect(currentButtonRegion?.region).toBe(MOCK_RADIO_BUTTON_REGIONS[1].region);
    expect(component.selectedRegion()).toBe(MOCK_RADIO_BUTTON_REGIONS[1].region);

    // Check if channels requested
    expect(radioStore.getSabaRadioChannels).toHaveBeenLastCalledWith(3);

    // Check if SetRegion button is disabled
    const button = fixture.nativeElement.querySelector('button');
    expect(button).toBeTruthy();
    expect(button?.disabled).toBe(true);
  });

  it('should set regions list to dropdown', async () => {
    // Get the mat-select element
    const matSelect: HTMLElement = fixture.nativeElement.querySelector('mat-select');
    expect(matSelect).toBeTruthy();

    // Click to open the mat-select dropdown
    matSelect.click();
    fixture.detectChanges();

    // Find mat-option elements in the overlay container
    const options = document.querySelectorAll('mat-option');
    expect(options.length).toBe(MOCK_RADIO_REGIONS_LIST.length);

    const optionTexts = Array.from(options).map(opt => opt.textContent?.trim());
    expect(optionTexts).toEqual(MOCK_RADIO_REGIONS_LIST);

    // Check if getRadioStationsByRegion called, if value is not empty
    const refreshedMatSelect: HTMLElement = fixture.nativeElement.querySelector('mat-select');
    expect(refreshedMatSelect.getAttribute('ng-reflect-model')).toBe(
      MOCK_RADIO_BUTTON_REGIONS[0].region,
    );

    // Check if radio stations panel is shown
    const radioStationsPanel = fixture.nativeElement.querySelector('.radio-stations-panel');
    expect(radioStationsPanel).toBeTruthy();

    // Check if SetRegion button is disabled
    const button = fixture.nativeElement.querySelector('button');
    expect(button).toBeTruthy();
    expect(button?.disabled).toBe(true);
  });

  it('should leave regions dropdown value empty and hide radio stations table if no region for button setup', async () => {
    // Arrange
    const matRadioButtons: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('mat-radio-button');
    expect(matRadioButtons.length).toBe(MOCK_RADIO_BUTTONS_LIST.length);

    // Fourth radio button has no region in MOCK_RADIO_BUTTONS_LIST
    const elementAsInput = matRadioButtons[3].querySelector(
      'input[type="radio"]',
    ) as HTMLInputElement;

    // Act
    elementAsInput.click();
    fixture.detectChanges();

    // Assert
    expect(component.selectedButton()).toBe(4);
    const currentButtonRegion = component.currentButtonRegion();
    expect(currentButtonRegion).toBeFalsy();
    expect(component.selectedRegion()).toBe('');

    // Check if mat-select contains empty value
    const matSelect: HTMLElement = fixture.nativeElement.querySelector('mat-select');
    expect(matSelect).toBeTruthy();
    matSelect.click();
    fixture.detectChanges();
    const refreshedMatSelect: HTMLElement = fixture.nativeElement.querySelector('mat-select');
    expect(refreshedMatSelect.getAttribute('ng-reflect-model')).toBeFalsy();

    // Check if no radio stations panel is shown
    const radioStationsPanel = fixture.nativeElement.querySelector('.radio-stations-panel');
    expect(radioStationsPanel).toBeFalsy();

    // Check if SetRegion button is disabled
    const button = fixture.nativeElement.querySelector('button');
    expect(button).toBeTruthy();
    expect(button?.disabled).toBe(true);
  });

  it('should set SaveRegion button enabled when new region for button selected', async () => {
    // Arrange
    const regionIndex = 2; // Berlin in the mock MOCK_RADIO_REGIONS_LIST

    const matSelect: HTMLElement = fixture.nativeElement.querySelector('mat-select');
    expect(matSelect).toBeTruthy();
    matSelect.click();
    fixture.detectChanges();

    const options = document.querySelectorAll('mat-option');
    expect(options.length).toBe(MOCK_RADIO_REGIONS_LIST.length);
    const optionToSelect = Array.from(options)[regionIndex] as HTMLElement;
    expect(optionToSelect).toBeTruthy();

    // Act
    optionToSelect.click();
    fixture.detectChanges();

    // Assert
    const refreshedMatSelect: HTMLElement = fixture.nativeElement.querySelector('mat-select');
    expect(refreshedMatSelect.getAttribute('ng-reflect-model')).toBe(
      MOCK_RADIO_REGIONS_LIST[regionIndex],
    );

    // Check if SetRegion button is enabled
    const button = fixture.nativeElement.querySelector('button');
    expect(button).toBeTruthy();
    expect(button?.disabled).toBe(false);

    // should request stations info list on Region select
    expect(radioStore.getRadioStationsByRegion).toHaveBeenLastCalledWith(
      MOCK_RADIO_REGIONS_LIST[regionIndex],
    );
  });

  it('should call appropriate method on SaveRegion button click', async () => {
    // Arrange
    const regionIndex = 2; // Berlin in the mock MOCK_RADIO_REGIONS_LIST

    const matSelect: HTMLElement = fixture.nativeElement.querySelector('mat-select');
    matSelect.click();
    fixture.detectChanges();

    const options = document.querySelectorAll('mat-option');
    expect(options.length).toBe(MOCK_RADIO_REGIONS_LIST.length);
    const optionToSelect = Array.from(options)[regionIndex] as HTMLElement;
    expect(optionToSelect).toBeTruthy();

    // Select new region
    optionToSelect.click();
    fixture.detectChanges();

    // Check if SetRegion button is enabled
    const button = fixture.nativeElement.querySelector('button');
    expect(button).toBeTruthy();
    expect(button?.disabled).toBe(false);

    // Act
    button.click();
    fixture.detectChanges();

    // Assert
    expect(radioStore.setRadioButtonRegion).toHaveBeenCalledWith({
      sabaRadioButton: 2,
      region: MOCK_RADIO_REGIONS_LIST[regionIndex],
    });

    // Should disable button back
    const buttonAfter = fixture.nativeElement.querySelector('button');
    expect(buttonAfter).toBeTruthy();
    expect(buttonAfter?.disabled).toBe(true);
  });

  it('should show radio stations list items', async () => {
    // Check if radio stations panel is shown by default, because mock data contains a region associated with default selected radio button
    const radioStationsRightPanel = fixture.nativeElement.querySelector(
      '.radio-stations-right-panel',
    );
    expect(radioStationsRightPanel).toBeTruthy();

    const stationsTableRows = Array.from(
      radioStationsRightPanel.querySelectorAll('tr'),
    ) as HTMLElement[];
    expect(stationsTableRows.length).toBe(MOCK_SAARLAND_RADIO_STATIONS.length);

    for (let i = 0; i < MOCK_SAARLAND_RADIO_STATIONS.length; i++) {
      const cells = stationsTableRows[i].querySelectorAll('td');
      expect(cells[0].innerHTML.trim()).toBe(`${MOCK_SAARLAND_RADIO_STATIONS[i].frequency} MHz`);
      expect(cells[1].innerHTML.trim()).toBe(MOCK_SAARLAND_RADIO_STATIONS[i].name);
    }

    // console.log((fixture.nativeElement as HTMLElement).innerHTML);
  });

  it('should disable radio station info list item on the left if it is present in the SABA Channels list on the right', async () => {
    // Check if radio stations panel is shown by default, because mock data contains a region associated with default selected radio button
    const radioStationsRightPanel = fixture.nativeElement.querySelector(
      '.radio-stations-right-panel',
    );
    expect(radioStationsRightPanel).toBeTruthy();

    const stationsTableRows = Array.from(
      radioStationsRightPanel.querySelectorAll('tr'),
    ) as HTMLElement[];
    expect(stationsTableRows.length).toBe(MOCK_SAARLAND_RADIO_STATIONS.length);

    for (let i = 0; i < MOCK_SAARLAND_RADIO_STATIONS.length; i++) {
      const radioName = MOCK_SAARLAND_RADIO_STATIONS[i].name;
      const channelForFrequency = MOCK_RADIO_CHANNELS.find(c => c.name === radioName);
      const rowDisabled = stationsTableRows[i].classList.contains('cdk-drag-disabled');
      if (channelForFrequency) {
        expect(rowDisabled).toBe(true);
      } else {
        expect(rowDisabled).toBe(false);
      }
    }
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

  it('should save new SABA Radio channel on drag-drop onto the channels list table on the left', async () => {
    const radioStationsRightPanel = fixture.nativeElement.querySelector(
      '.radio-stations-right-panel',
    );

    const radioStationsLeftPanel = fixture.nativeElement.querySelector(
      '.radio-stations-left-panel',
    );

    // Dragging rp[1] -> lp[0],
    const radioChannelIndex = 0;
    const radioStationIndex = 1;
    const radioStation = MOCK_SAARLAND_RADIO_STATIONS[radioStationIndex];
    const event = {
      previousIndex: radioStationIndex,
      currentIndex: radioChannelIndex,
      previousContainer: radioStationsRightPanel,
      container: radioStationsLeftPanel,
      item: {
        data: radioStation,
      },
    };

    // Act
    component.sabaChannelDrop(event as CdkDragDrop<RadioStationInfo>);

    // Assert
    expect(radioStore.setSabaRadioChannel).toHaveBeenCalledWith({
      sabaFrequency: MOCK_SABA_CHANNELS_FREQUENCIES[radioChannelIndex],
      name: radioStation.name,
      button: 2,
      region: radioStation.region,
      streamUrl: radioStation.stationStreamUrl,
    });
    expect(radioStore.deleteSabaRadioChannel).not.toHaveBeenCalled();
  });

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
      region: channel?.region,
      radioLogoBase64: channel?.radioLogoBase64,
    });
  });
});
