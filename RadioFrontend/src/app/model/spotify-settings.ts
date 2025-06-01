export interface SpotifySettings {
  clientId?: string;
  clientSecret?: string;
  redirectUrl?: string;
  authToken?: string;
  authTokenExpiration?: number;
  refreshToken?: string;
  deviceName?: string;
  playlistName?: string;
}
