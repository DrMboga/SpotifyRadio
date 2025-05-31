import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-spotify-login-code',
  imports: [],
  templateUrl: './spotify-login-code.component.html',
  styleUrl: './spotify-login-code.component.css',
})
export class SpotifyLoginCodeComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router); // Redirect to /spotify

  private readonly code$ = this.route.queryParams.pipe(map(params => params['code'] as string));
  code = toSignal(this.code$);
}
