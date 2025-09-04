export interface RadioStationInfo {
  name: string;
  genres?: string;
  detailsUrl: string;
  country: string;
  regionInfo?: string;
  rating?: number;
  likes?: number;
  dislikes?: number;
  stationDescription?: string;
  stationWebPage?: string;
  stationImageUrl?: string;
  stationStreamUrl?: string;
  stationProcessed: boolean;
}
