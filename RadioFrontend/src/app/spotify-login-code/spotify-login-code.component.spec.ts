import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpotifyLoginCodeComponent } from './spotify-login-code.component';
import { signal } from '@angular/core';
import { SpotifySettings } from '../model/spotify-settings';
import { SpotifyStore } from '../store/spotify.store';
import { ActivatedRoute } from '@angular/router';
import { MOCK_SPOTIFY_SETTINGS } from '../mock/spotify-mock';
import { of } from 'rxjs';

describe('SpotifyLoginCodeComponent', () => {
  let component: SpotifyLoginCodeComponent;
  let fixture: ComponentFixture<SpotifyLoginCodeComponent>;

  let spotifyStoreMock = {
    settings: signal<SpotifySettings>(MOCK_SPOTIFY_SETTINGS),
    getAndSaveAuthenticationToken: jest.fn(),
  };

  let mockRoute = {
    queryParams: of({ code: 'fakeAuthCode' }),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpotifyLoginCodeComponent],
      providers: [
        { provide: SpotifyStore, useValue: spotifyStoreMock },
        { provide: ActivatedRoute, useValue: mockRoute },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SpotifyLoginCodeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should request and save authentication token', async () => {
    expect(spotifyStoreMock.getAndSaveAuthenticationToken).toHaveBeenCalledWith({
      code: 'fakeAuthCode',
      clientId: MOCK_SPOTIFY_SETTINGS.clientId,
      clientSecret: MOCK_SPOTIFY_SETTINGS.clientSecret,
    });
  });
});
