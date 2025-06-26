import { SpotifySettings } from '../model/spotify-settings';
import { SpotifyDevice } from '../model/spotify-device';
import { PlayListItem } from '../model/spotify-playlist-item';

export const MOCK_SPOTIFY_SETTINGS: SpotifySettings = {
  clientId: 'mockClientId',
  clientSecret: 'mockClientSecret',
  redirectUrl: 'https://mock-redirect.com',
};

export const MOCK_SPOTIFY_DEVICES: SpotifyDevice[] = [
  {
    id: 'device1',
    name: 'Device 1',
    is_active: true,
    is_private_session: false,
    is_restricted: false,
    supports_volume: false,
    type: '',
    volume_percent: 0,
  },
  {
    id: 'device2',
    name: 'Device 2',
    is_active: true,
    is_private_session: false,
    is_restricted: false,
    supports_volume: false,
    type: '',
    volume_percent: 0,
  },
];

export const MOCK_SPOTIFY_PLAYLISTS: PlayListItem[] = [
  {
    id: 'playlist1',
    name: 'Playlist 1',
  },
  {
    id: 'playlist2',
    name: 'Playlist 2',
  },
  {
    id: 'playlist3',
    name: 'Playlist 3',
  },
];
