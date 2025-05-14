import { Component, inject } from '@angular/core';
import { BackendService } from '../services/backend.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatListModule } from '@angular/material/list';

@Component({
  selector: 'app-spotify-settings',
  imports: [MatListModule],
  templateUrl: './spotify-settings.component.html',
  styleUrl: './spotify-settings.component.css',
})
export class SpotifySettingsComponent {
  private readonly backend = inject(BackendService);

  /**
   * @deprecated For demo purpose
   */
  forecast = toSignal(this.backend.forecast());
}
