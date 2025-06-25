import { RadioChannel } from '../model/radio-channel';
import { RadioButtonInfo } from '../model/radio-button-info';
import { RadioButtonRegion } from '../model/radio-button-region';
import { RadioStationInfo } from '../model/radio-station-info';

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

export const MOCK_SABA_CHANNELS_FREQUENCIES: number[] = [87, 88, 89, 90, 91];

export const MOCK_SAARLAND_RADIO_STATIONS: RadioStationInfo[] = [
  {
    name: 'Classic Rock Radio',
    frequency: 92.9,
    detailsUrl: '/radio/classic-rock-radio-germany-415590/',
    region: 'Saarland',
    stationUrl: 'http://www.classicrock-radio.de/Startseite.phtml',
    stationImageUrl: 'https://static2.mytuner.mobi/media/tvos_radios/QUB8HFFbk9.jpg',
    stationStreamUrl: 'https://internetradio.salue.de:8443/classicrock.mp3',
  },
  {
    name: 'bigFM Saarland',
    frequency: 94.2,
    detailsUrl: '/radio/bigfm-saarland-406863/',
    region: 'Saarland',
    stationUrl: 'http://www.bigfm-saarland.de/',
    stationImageUrl:
      'https://static2.mytuner.mobi/media/tvos_radios/863/bigfm-saarland.15ff7ba6.jpg',
    stationStreamUrl: 'https://audiotainment-sw.streamabc.net/atsw-bigfm-aac-128-6355201',
  },
  {
    name: 'CityRadio Neunkirchen',
    frequency: 94.6,
    detailsUrl: '/radio/cityradio-neunkirchen-483352/',
    region: 'Saarland',
    stationUrl: 'https://cityradio.saarland/neunkirchen/',
    stationImageUrl: 'https://static2.mytuner.mobi/media/tvos_radios/GTVMTUaxfj.png',
    stationStreamUrl: 'https://stream.radiogroup.de/cityradio-neunkirchen/mp3-192/',
  },
  {
    name: 'SR 3 Saarlandwelle',
    frequency: 95.5,
    detailsUrl: '/radio/sr-3-saarlandwelle-407590/',
    region: 'Saarland',
    stationUrl: 'http://www.sr-online.de/',
    stationImageUrl:
      'https://static2.mytuner.mobi/media/tvos_radios/590/sr-3-saarlandwelle.56508102.png',
    stationStreamUrl: 'https://liveradio.sr.de/sr/sr3/mp3/128/stream.mp3',
  },
  {
    name: 'SR 1 Europawelle',
    frequency: 98.2,
    detailsUrl: '/radio/sr-1-europawelle-407588/',
    region: 'Saarland',
    stationUrl: 'http://www.sr.de/sr/sr1/index.html',
    stationImageUrl:
      'https://static2.mytuner.mobi/media/tvos_radios/588/sr-1-europawelle.e8e640f6.png',
    stationStreamUrl: 'https://liveradio.sr.de/sr/sr1/mp3/128/stream.mp3',
  },
];

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
