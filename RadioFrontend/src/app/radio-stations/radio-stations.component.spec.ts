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
  MOCK_RADIO_REGIONS_LIST,
} from '../mock/radio-mock';

describe('RadioStationsComponent', () => {
  let component: RadioStationsComponent;
  let fixture: ComponentFixture<RadioStationsComponent>;
  let radioStore = {
    radioButtonsList: signal<RadioButtonInfo[]>(MOCK_RADIO_BUTTONS_LIST),
    regionsList: signal<string[]>(MOCK_RADIO_REGIONS_LIST),
    radioButtonRegions: signal<RadioButtonRegion[]>(MOCK_RADIO_BUTTON_REGIONS),
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
  });

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
