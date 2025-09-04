export interface RadioChannel {
  button: number;
  stationDetailsUrl: string;
  name: string;
  sabaFrequency: number;
  streamUrl?: string;
  radioLogoBase64?: string;
  country?: string;
  countryFlagBase64?: string;
}
