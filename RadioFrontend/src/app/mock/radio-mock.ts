import { RadioChannel } from '../model/radio-channel';
import { RadioButtonInfo } from '../model/radio-button-info';
import { RadioButtonRegion } from '../model/radio-button-region';

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

export const MOCK_RADIO_REGIONS_LIST: string[] = [
  'Baden-Württemberg',
  'Bavaria',
  'Berlin',
  'Brandenburg',
];

export const MOCK_RADIO_BUTTON_REGIONS: RadioButtonRegion[] = [
  {
    sabaRadioButton: 2,
    region: 'Saarland',
  },
  {
    sabaRadioButton: 3,
    region: 'Bavaria',
  },
];
