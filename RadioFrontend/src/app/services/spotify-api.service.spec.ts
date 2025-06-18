import { TestBed } from '@angular/core/testing';

import { SpotifyApiService } from './spotify-api.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('SpotifyApiService', () => {
  let service: SpotifyApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SpotifyApiService);
  });

  // https://angular.dev/guide/http/testing

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
