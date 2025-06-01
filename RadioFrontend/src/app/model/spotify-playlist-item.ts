export interface ExternalUrl {
  spotify: string;
}

export interface PlayListImage {
  height: number;
  width: number;
  url: string;
}

export interface PlaylistTracksInfo {
  href: string;
  total: number;
}

export interface PlayListItem {
  id: string;
  name: string;
  external_urls?: ExternalUrl;
  images?: PlayListImage[];
  tracks?: PlaylistTracksInfo;
}
