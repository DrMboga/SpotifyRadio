export interface SpotifySettings {
  clientId?: string;
  clientSecret?: string;
  redirectUrl?: string;
  authToken?: string;
  authTokenExpiration?: Date;
  refreshToken?: string;
  deviceName?: string;
  playlistName?: string;
}
