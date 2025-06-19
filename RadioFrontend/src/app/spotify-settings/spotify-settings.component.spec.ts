import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpotifySettingsComponent } from './spotify-settings.component';
import { SpotifyStore } from '../store/spotify.store';
import { SpotifySettings } from '../model/spotify-settings';
import { signal } from '@angular/core';
import { SpotifyDevice } from '../model/spotify-device';
import { PlayListItem } from '../model/spotify-playlist-item';

describe('SpotifySettingsComponent', () => {
  let component: SpotifySettingsComponent;
  let fixture: ComponentFixture<SpotifySettingsComponent>;

  let spotifyStoreMock = {
    settings: signal<SpotifySettings>({}),
    devices: signal<SpotifyDevice[]>([]),
    playLists: signal<PlayListItem[]>([]),
    isAuthorized: signal<boolean>(false),
    spotifyLoginUrl: signal<string>(''),
    readSpotifyDevices: jest.fn(),
    readPlayLists: jest.fn(),
    saveSpotifySettings: jest.fn(),
  };

  beforeEach(async () => {
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
});
