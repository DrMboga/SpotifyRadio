import { RadioChannel } from '../model/radio-channel';
import { RadioButtonInfo } from '../model/radio-button-info';
import { RadioCountry } from '../model/radio-country';
import { RadioStationsCacheStatus } from '../model/radio-stations-cache-status';
import { RadioStationInfo } from '../model/radio-station-info';

export const MOCK_RADIO_CHANNELS_LIST: RadioChannel[] = [
  {
    button: 1,
    country: 'Germany',
    stationDetailsUrl: '/radio/g1/',
    name: 'Channel 1',
    sabaFrequency: 88,
  },
  {
    button: 1,
    country: 'Germany',
    stationDetailsUrl: '/radio/g2/',
    name: 'Channel 2',
    sabaFrequency: 89,
  },
  {
    button: 1,
    country: 'Germany',
    stationDetailsUrl: '/radio/g3/',
    name: 'Channel 3',
    sabaFrequency: 90,
  },
  {
    button: 1,
    country: 'Germany',
    stationDetailsUrl: '/radio/g4/',
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

export const MOCK_STATIONS_CACHE_STATUS: RadioStationsCacheStatus = {
  totalStations: 48,
  processedCount: 12,
};

export const MOCK_RADIO_STATION_INFOS: RadioStationInfo[] = [
  {
    name: 'Radio Latina',
    genres: 'Latino',
    detailsUrl: '/radio/radio-latina-414011/',
    country: 'Luxembourg',
    regionInfo: 'FM 101.2 MHz',
    rating: 0,
    likes: 54,
    dislikes: 3,
    stationDescription: '',
    stationWebPage: 'http://www.radiolatina.lu/',
    stationImageUrl: 'https://static2.mytuner.mobi/media/tvos_radios/JqTfBQ2HUn.png',
    stationStreamUrl: 'https://ice.creacast.com/radio-latina-lu-mp3',
    stationProcessed: true,
  },
  {
    name: 'RTL Radio Lëtzebuerg 88.9',
    genres: 'News | Sports | Talk',
    detailsUrl: '/radio/rtl-radio-letzebuerg-889-401528/',
    country: 'Luxembourg',
    regionInfo: 'FM 88.9 MHz | FM 92.5 MHz',
    rating: 82,
    likes: 45,
    dislikes: 10,
    stationDescription:
      'RTL Radio Lëtzebuerg, Full Service Station and the #1 Radio Station in Luxembourg',
    stationWebPage: 'https://www.rtl.lu/radio',
    stationImageUrl: 'https://static2.mytuner.mobi/media/tvos_radios/V87yDVTUDG.png',
    stationStreamUrl: 'https://sc.rtl.lu/rtl',
    stationProcessed: true,
  },
  {
    name: "L'essentiel Radio",
    genres: 'Pop Music',
    detailsUrl: '/radio/lessentiel-radio-435687/',
    country: 'Luxembourg',
    regionInfo: 'FM 107.7 MHz',
    rating: 0,
    likes: 27,
    dislikes: 7,
    stationDescription: '',
    stationWebPage: 'http://www.lessentiel.lu/fr/',
    stationImageUrl: 'https://static2.mytuner.mobi/media/tvos_radios/8qxczt8v7fct.png',
    stationStreamUrl: 'https://lessentielradio.ice.infomaniak.ch/lessentielradio-128.mp3',
    stationProcessed: true,
  },
];

export const MOCK_SABA_CHANNELS_FREQUENCIES: number[] = [87, 88, 89, 90, 91];

export const MOCK_RADIO_CHANNELS: RadioChannel[] = [
  {
    button: 2,
    stationDetailsUrl: '/radio/klassik-radio-399934/',
    name: 'Klassik Radio',
    sabaFrequency: 88,
    streamUrl: 'https://stream.klassikradio.de/national/mp3-192/mytuner',
    country: 'Germany',
  },
  {
    button: 2,
    stationDetailsUrl: '/radio/radio-latina-414011/',
    name: 'Radio Latina',
    sabaFrequency: 89,
    streamUrl: 'https://ice.creacast.com/radio-latina-lu-mp3',
    country: 'Luxembourg',
  },
];
