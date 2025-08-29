import { TestBed } from '@angular/core/testing';

import { BackendService } from './backend.service';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { MOCK_SPOTIFY_SETTINGS } from '../mock/spotify-mock';
import { MOCK_RADIO_BUTTONS_LIST, MOCK_RADIO_CHANNELS } from '../mock/radio-mock';

// https://angular.dev/guide/http/testing
describe('BackendService', () => {
  let service: BackendService;
  let httpTesting: HttpTestingController;
  let baseUrl = location.origin;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    service = TestBed.inject(BackendService);
  });

  afterEach(() => {
    // Verify that none of the tests make any extra HTTP requests.
    TestBed.inject(HttpTestingController).verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should read spotify settings endpoint', async () => {
    const spotifySettingsPromise = firstValueFrom(service.readSpotifySettings());
    const pendingRequest = httpTesting.expectOne(`${baseUrl}/spotify-settings`);

    expect(pendingRequest.request.method).toBe('GET');

    pendingRequest.flush(MOCK_SPOTIFY_SETTINGS);

    expect(await spotifySettingsPromise).toBe(MOCK_SPOTIFY_SETTINGS);

    httpTesting.verify();
  });

  it('should set spotify settings', async () => {
    const spotifySettingsPromise = firstValueFrom(
      service.saveSpotifySettings(MOCK_SPOTIFY_SETTINGS),
    );
    const pendingRequest = httpTesting.expectOne(`${baseUrl}/spotify-settings`);

    expect(pendingRequest.request.method).toBe('PATCH');
    expect(pendingRequest.request.body).toBe(MOCK_SPOTIFY_SETTINGS);

    pendingRequest.flush(MOCK_SPOTIFY_SETTINGS);

    expect(await spotifySettingsPromise).toBe(MOCK_SPOTIFY_SETTINGS);

    httpTesting.verify();
  });

  it('should read radio buttons', async () => {
    const responsePromise = firstValueFrom(service.readRadioButtons());
    const pendingRequest = httpTesting.expectOne(`${baseUrl}/radio-buttons`);

    expect(pendingRequest.request.method).toBe('GET');

    pendingRequest.flush(MOCK_RADIO_BUTTONS_LIST);

    expect(await responsePromise).toBe(MOCK_RADIO_BUTTONS_LIST);

    httpTesting.verify();
  });

  it('should read channels by button', async () => {
    const button = 2;
    const responsePromise = firstValueFrom(service.getSabaChannels(button));
    const pendingRequest = httpTesting.expectOne(
      `${baseUrl}/radio-stations-by-button?button=${button}`,
    );

    expect(pendingRequest.request.method).toBe('GET');

    pendingRequest.flush(MOCK_RADIO_CHANNELS);

    expect(await responsePromise).toBe(MOCK_RADIO_CHANNELS);

    httpTesting.verify();
  });

  it('should set SABA channel', async () => {
    const responsePromise = firstValueFrom(service.setSabaChannel(MOCK_RADIO_CHANNELS[0]));
    const pendingRequest = httpTesting.expectOne(`${baseUrl}/radio-stations-by-button`);

    expect(pendingRequest.request.method).toBe('POST');
    expect(pendingRequest.request.body).toBe(MOCK_RADIO_CHANNELS[0]);

    pendingRequest.flush(MOCK_RADIO_CHANNELS[0]);

    expect(await responsePromise).toBe(MOCK_RADIO_CHANNELS[0]);

    httpTesting.verify();
  });

  it('should delete SABA channel', async () => {
    const button = 2;
    const frequency = 88;
    const responsePromise = firstValueFrom(service.deleteSabaChannel(button, frequency));
    const pendingRequest = httpTesting.expectOne(
      `${baseUrl}/radio-stations-by-button?button=${button}&sabaFrequency=${frequency}`,
    );

    expect(pendingRequest.request.method).toBe('DELETE');

    pendingRequest.flush('OK');

    expect(await responsePromise).toBe(void 0);

    httpTesting.verify();
  });
});
