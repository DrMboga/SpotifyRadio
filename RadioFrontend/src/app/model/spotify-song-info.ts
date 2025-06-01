export interface AlbumImage {
  url?: string;
  height?: number;
  width?: number;
}

export interface AlbumInfo {
  images?: AlbumImage[];
  name?: string;
}

export interface ArtistInfo {
  name?: string;
}

export interface SongItem {
  album?: AlbumInfo;
  artists?: ArtistInfo[];
  duration_ms?: number;
  name?: string;
}

export interface SongInfo {
  progress_ms?: number;
  item?: SongItem;
}
