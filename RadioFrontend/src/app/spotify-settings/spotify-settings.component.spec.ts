import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpotifySettingsComponent } from './spotify-settings.component';
import { SpotifyStore } from '../store/spotify.store';
import { SpotifySettings } from '../model/spotify-settings';
import { signal } from '@angular/core';
import { SpotifyDevice } from '../model/spotify-device';
import { PlayListItem } from '../model/spotify-playlist-item';
import {
  MOCK_SPOTIFY_DEVICES,
  MOCK_SPOTIFY_PLAYLISTS,
  MOCK_SPOTIFY_SETTINGS,
} from '../mock/spotify-mock';

describe('SpotifySettingsComponent', () => {
  let component: SpotifySettingsComponent;
  let fixture: ComponentFixture<SpotifySettingsComponent>;

  let spotifyStoreMock: any;

  beforeEach(async () => {
    spotifyStoreMock = {
      settings: signal<SpotifySettings>({}),
      devices: signal<SpotifyDevice[]>([]),
      playLists: signal<PlayListItem[]>([]),
      isAuthorized: signal<boolean>(false),
      spotifyLoginUrl: signal<string>(''),
      readSpotifyDevices: jest.fn(),
      readPlayLists: jest.fn(),
      saveSpotifySettings: jest.fn(),
    };
    await TestBed.configureTestingModule({
      imports: [SpotifySettingsComponent],
      providers: [{ provide: SpotifyStore, useValue: spotifyStoreMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(SpotifySettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set empty client id and secret fields, Authorise button enabled and no devices and playlist selects when settings is empty', () => {
    const inputs: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('input');
    const buttons: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('button');
    const selects: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('mat-select');

    expect(selects.length).toBe(0);

    expect(buttons.length).toBe(1);
    expect((buttons[0] as HTMLButtonElement).disabled).toBe(false);

    expect(inputs.length).toBe(2);
    for (const inputHtml of inputs) {
      const input = inputHtml as HTMLInputElement;
      expect(input.value).toBe('');
    }
  });

  it('should disable authorized button when isAuthorized flag is set', () => {
    spotifyStoreMock.isAuthorized.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');

    expect(button.disabled).toBe(true);
  });

  it('should save client id and secret on save button', () => {
    // Arrange
    const clientId = 'fakeClientId';
    const clientSecret = 'fakeClientSecret';

    const inputs: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('input');
    expect(inputs.length).toBe(2);
    const clientIdInput = inputs[0] as HTMLInputElement;
    const clientSecretInput = inputs[1] as HTMLInputElement;
    clientIdInput.value = clientId;
    clientIdInput.dispatchEvent(new Event('input'));
    clientSecretInput.value = clientSecret;
    clientSecretInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBe(false);

    // Act
    button.click();
    fixture.detectChanges();

    // Assert
    expect(spotifyStoreMock.saveSpotifySettings).toHaveBeenCalledWith({
      clientId,
      clientSecret,
    });
  });

  it('should show mat selects for devices and playlist when client id and secret are not empty', () => {
    // Arrange
    spotifyStoreMock.settings.set(MOCK_SPOTIFY_SETTINGS);
    spotifyStoreMock.devices.set(MOCK_SPOTIFY_DEVICES);
    spotifyStoreMock.playLists.set(MOCK_SPOTIFY_PLAYLISTS);

    // Act
    fixture.detectChanges();

    // Assert
    const selects: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('mat-select');
    const buttons: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('button');
    expect(selects.length).toBe(2);
    expect(buttons.length).toBe(2);
  });

  it('should save device and play list on Save button click', () => {
    // Arrange
    spotifyStoreMock.settings.set(MOCK_SPOTIFY_SETTINGS);
    spotifyStoreMock.devices.set(MOCK_SPOTIFY_DEVICES);
    spotifyStoreMock.playLists.set(MOCK_SPOTIFY_PLAYLISTS);
    fixture.detectChanges();
    const selects: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('mat-select');
    const buttons: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('button');
    expect(selects.length).toBe(2);
    // 2 buttons should have shown - "Save device and playlist" and "Authorize"
    expect(buttons.length).toBe(2);

    const devicesSelect = selects[0] as HTMLElement;
    devicesSelect.click();
    fixture.detectChanges();
    const devicesMatOptions = document.querySelectorAll('mat-option');
    expect(devicesMatOptions.length).toBe(MOCK_SPOTIFY_DEVICES.length);

    const playListsSelect = selects[1] as HTMLElement;
    playListsSelect.click();
    fixture.detectChanges();
    const options = document.querySelectorAll('mat-option');
    const playlistsMatOptions = Array.from(options).slice(
      devicesMatOptions.length,
      options.length,
    ) as HTMLElement[];
    expect(playlistsMatOptions.length).toBe(MOCK_SPOTIFY_PLAYLISTS.length);

    const selectedDeviceIndex = 1;
    const selectedPlaylistIndex = 2;

    // Act
    // Select device in the mat-select input
    (devicesMatOptions[selectedDeviceIndex] as HTMLElement).click();
    fixture.detectChanges();
    // Select playlist in mat-select input
    playlistsMatOptions[selectedPlaylistIndex].click();
    fixture.detectChanges();

    // Click Save button
    buttons[0].click();
    fixture.detectChanges();

    // Assert
    expect(spotifyStoreMock.saveSpotifySettings).toHaveBeenCalledWith({
      ...MOCK_SPOTIFY_SETTINGS,
      deviceName: MOCK_SPOTIFY_DEVICES[selectedDeviceIndex].name,
      playlistName: MOCK_SPOTIFY_PLAYLISTS[selectedPlaylistIndex].name,
    });
  });
});
