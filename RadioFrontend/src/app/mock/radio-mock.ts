import { RadioChannel } from '../model/radio-channel';
import { RadioButtonInfo } from '../model/radio-button-info';
import { RadioCountry } from '../model/radio-country';

export const MOCK_RADIO_CHANNELS_LIST: RadioChannel[] = [
  {
    button: 1,
    region: 'Bavaria',
    name: 'Channel 1',
    sabaFrequency: 88,
  },
  {
    button: 1,
    region: 'Bavaria',
    name: 'Channel 2',
    sabaFrequency: 89,
  },
  {
    button: 1,
    region: 'Bavaria',
    name: 'Channel 3',
    sabaFrequency: 90,
  },
  {
    button: 1,
    region: 'Bavaria',
    name: 'Channel 4',
    sabaFrequency: 91,
  },
];

export const MOCK_RADIO_BUTTONS_LIST: RadioButtonInfo[] = [
  {
    button: 1,
    buttonLabel: 'L',
    buttonDescription: 'Long waves button which has a L sign on a SABA panel',
  },
  {
    button: 2,
    buttonLabel: 'M',
    buttonDescription: 'Middle waves button which has a M sign on a SABA panel',
  },
  {
    button: 3,
    buttonLabel: 'K',
    buttonDescription: 'Short waves button which has a K sign on a SABA panel',
  },
  {
    button: 4,
    buttonLabel: 'U',
    buttonDescription: 'FM waves button which has an U sign on a SABA panel',
  },
];

export const MOCK_RADIO_COUNTRIES: RadioCountry[] = [
  {
    country: 'Germany',
    flagImageUrl: 'https://static.mytuner.mobi/media/countries/de.png',
    detailsUrl: 'https://mytuner-radio.com/radio/country/germany-stations/frequency/fm',
  },
  {
    country: 'Netherlands',
    flagImageUrl: 'https://static.mytuner.mobi/media/countries/nl.png',
    detailsUrl: 'https://mytuner-radio.com/radio/country/netherlands-stations/frequency/fm',
  },
];

export const MOCK_SABA_CHANNELS_FREQUENCIES: number[] = [87, 88, 89, 90, 91];

export const MOCK_RADIO_CHANNELS: RadioChannel[] = [
  {
    button: 2,
    region: 'Saarland',
    name: 'Classic Rock Radio',
    sabaFrequency: 88,
    radioLogoBase64: 'data:image/jpg;base64,...',
  },
  {
    button: 2,
    region: 'Saarland',
    name: 'SR 1 Europawelle',
    sabaFrequency: 89,
    radioLogoBase64: 'data:image/jpg;base64,...',
  },
];
