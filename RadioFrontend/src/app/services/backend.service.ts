import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Forecast } from '../model/forecast';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class BackendService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.production ? location.origin : 'http://localhost:5262';

  /**
   * @deprecated For demo purpose
   */
  forecast(): Observable<Forecast[]> {
    console.log('production:', environment.production);
    const url = `${this.baseUrl}/weatherforecast`;
    return this.http.get<Forecast[]>(url);
  }
}
