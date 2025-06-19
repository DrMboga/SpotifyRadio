import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpotifyLoginCodeComponent } from './spotify-login-code.component';
import { signal } from '@angular/core';
import { SpotifySettings } from '../model/spotify-settings';
import { SpotifyStore } from '../store/spotify.store';
import { RouterModule } from '@angular/router';

describe('SpotifyLoginCodeComponent', () => {
  let component: SpotifyLoginCodeComponent;
  let fixture: ComponentFixture<SpotifyLoginCodeComponent>;

  let spotifyStoreMock = {
    settings: signal<SpotifySettings>({}),
    getAndSaveAuthenticationToken: jest.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpotifyLoginCodeComponent, RouterModule.forRoot([])],
      providers: [{ provide: SpotifyStore, useValue: spotifyStoreMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(SpotifyLoginCodeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
