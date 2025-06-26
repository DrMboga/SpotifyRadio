import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { SpotifyStore } from './spotify.store';
import { BackendService } from '../services/backend.service';
import { SpotifyApiService } from '../services/spotify-api.service';
import {
  MOCK_SPOTIFY_DEVICES,
  MOCK_SPOTIFY_PLAYLISTS,
  MOCK_SPOTIFY_SETTINGS,
} from '../mock/spotify-mock';
import { of, Subject } from 'rxjs';
import { SpotifySettings } from '../model/spotify-settings';
import { jest } from '@jest/globals';

// https://ngrx.io/guide/signals/signal-store/testing#rxmethod

describe('spotifyStore', () => {
  const mockToken = 'mockToken';
  const mockExpiration = new Date(2025, 5, 26, 15, 44, 0).getTime();
  const mockRefreshToken = 'mockRefreshToken';
  const mockRedirectUri = `${location.origin}/spotify-login`;

  let store: any;
  let backend = {
    readSpotifySettings: jest.fn().mockReturnValue(of(MOCK_SPOTIFY_SETTINGS)),
    // @ts-ignore
    saveSpotifySettings: jest.fn().mockImplementation((settings: SpotifySettings) => of(settings)),
  };
  let spotifyApi = {
    getToken: jest
      .fn()
      .mockReturnValue(
        of({ accessToken: mockToken, expiration: mockExpiration, refreshToken: mockRefreshToken }),
      ),
    getDevices: jest.fn().mockReturnValue(of(MOCK_SPOTIFY_DEVICES)),
    getPlayLists: jest.fn().mockReturnValue(of(MOCK_SPOTIFY_PLAYLISTS)),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: BackendService, useValue: backend },
        { provide: SpotifyApiService, useValue: spotifyApi },
      ],
    });

    store = TestBed.inject(SpotifyStore);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should set spotify settings from backend on init', () => {
    expect(backend.readSpotifySettings).toHaveBeenCalled();
    expect(store.settings()).toEqual(MOCK_SPOTIFY_SETTINGS);
    expect(store.isAuthorized()).toBe(false);
    expect(store.spotifyLoginUrl()).toBe(
      'https://accounts.spotify.com/authorize/?client_id=mockClientId&redirect_uri=http://localhost/spotify-login&response_type=code&show_dialog=true&scope=user-read-private user-read-email user-modify-playback-state user-read-playback-position user-library-read streaming user-read-playback-state user-read-recently-played playlist-read-private',
    );
  });

  it('should save spotify settings', fakeAsync(() => {
    const settingsToSave: SpotifySettings = {
      ...MOCK_SPOTIFY_SETTINGS,
      authToken: mockToken,
      refreshToken: mockRefreshToken,
      authTokenExpiration: mockExpiration,
      deviceName: 'mockDevice',
      playlistName: 'mockPlaylistName',
    };
    const settingsToSaveSubject$ = new Subject<SpotifySettings>();
    store.saveSpotifySettings(settingsToSaveSubject$);

    // Act
    settingsToSaveSubject$.next(settingsToSave);
    tick(100);

    // Assert
    expect(backend.saveSpotifySettings).toHaveBeenCalledWith(settingsToSave);
    expect(store.settings()).toEqual(settingsToSave);
  }));

  it('should return isAuthorized "true" if token is not expired', fakeAsync(() => {
    const settingsToSave: SpotifySettings = {
      ...MOCK_SPOTIFY_SETTINGS,
      authToken: mockToken,
      refreshToken: mockRefreshToken,
      authTokenExpiration: new Date().getTime() + 60000,
    };
    const settingsToSaveSubject$ = new Subject<SpotifySettings>();
    store.saveSpotifySettings(settingsToSaveSubject$);

    // Act
    settingsToSaveSubject$.next(settingsToSave);
    tick(100);

    // Assert
    expect(store.isAuthorized()).toBe(true);
  }));

  it('should return isAuthorized "false" if token is expired', fakeAsync(() => {
    const settingsToSave: SpotifySettings = {
      ...MOCK_SPOTIFY_SETTINGS,
      authToken: mockToken,
      refreshToken: mockRefreshToken,
      authTokenExpiration: new Date().getTime() - 60000,
    };
    const settingsToSaveSubject$ = new Subject<SpotifySettings>();
    store.saveSpotifySettings(settingsToSaveSubject$);

    // Act
    settingsToSaveSubject$.next(settingsToSave);
    tick(100);

    // Assert
    expect(store.isAuthorized()).toBe(false);
  }));

  it('should get and save Authentication Token', fakeAsync(() => {
    const authCodeInput = {
      code: 'fakeAuthCode',
      clientId: 'mockClientId',
      clientSecret: 'mockClientSecret',
    };

    const authCodeInputSubject$ = new Subject<{
      code: string;
      clientId: string;
      clientSecret: string;
    }>();

    // Act
    store.getAndSaveAuthenticationToken(authCodeInputSubject$);
    authCodeInputSubject$.next(authCodeInput);
    tick(100);

    // Assert
    expect(spotifyApi.getToken).toHaveBeenCalledWith(
      'fakeAuthCode',
      mockRedirectUri,
      'mockClientId',
      'mockClientSecret',
    );

    expect(backend.saveSpotifySettings).toHaveBeenCalledWith({
      authToken: mockToken,
      authTokenExpiration: mockExpiration * 1000,
      clientId: 'mockClientId',
      clientSecret: 'mockClientSecret',
      redirectUrl: mockRedirectUri,
      refreshToken: mockRefreshToken,
    });

    expect(store.settings()).toEqual({
      clientId: 'mockClientId',
      clientSecret: 'mockClientSecret',
      authToken: mockToken,
      authTokenExpiration: mockExpiration * 1000,
      redirectUrl: mockRedirectUri,
      refreshToken: mockRefreshToken,
    });
  }));

  it('should read spotify devices list', fakeAsync(() => {
    const authTokenSubject$ = new Subject<string>();

    store.readSpotifyDevices(authTokenSubject$);
    authTokenSubject$.next(mockToken);
    tick(100);

    // Assert
    expect(spotifyApi.getDevices).toHaveBeenCalledWith(mockToken);
    expect(store.devices()).toEqual(MOCK_SPOTIFY_DEVICES);
  }));

  it('should read spotify playlists list', fakeAsync(() => {
    const authTokenSubject$ = new Subject<string>();

    store.readPlayLists(authTokenSubject$);
    authTokenSubject$.next(mockToken);
    tick(100);

    // Assert
    expect(spotifyApi.getPlayLists).toHaveBeenCalledWith(mockToken);
    expect(store.playLists()).toEqual(MOCK_SPOTIFY_PLAYLISTS);
  }));
});
