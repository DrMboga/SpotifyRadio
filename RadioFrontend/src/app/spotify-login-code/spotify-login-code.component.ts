import { Component, computed, effect, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { SpotifyStore } from '../store/spotify.store';

@Component({
  selector: 'app-spotify-login-code',
  imports: [],
  templateUrl: './spotify-login-code.component.html',
  styleUrl: './spotify-login-code.component.css',
})
export class SpotifyLoginCodeComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router); // Redirect to /spotify
  private readonly spotifyStore = inject(SpotifyStore);

  private readonly code$ = this.route.queryParams.pipe(map(params => params['code'] as string));
  code = toSignal(this.code$);

  private authRequestParameters = computed(() => {
    const code = this.code();
    const settings = this.spotifyStore.settings();
    const clientId = settings.clientId;
    const clientSecret = settings.clientSecret;
    if (code && clientId && clientSecret) {
      return {
        code,
        clientId,
        clientSecret,
      };
    }
    return undefined;
  });

  constructor() {
    effect(() => {
      const params = this.authRequestParameters();
      if (params) {
        this.spotifyStore.getAndSaveAuthenticationToken(params);
        this.router.navigate(['/spotify']).then(r => {});
      }
    });
  }
}
