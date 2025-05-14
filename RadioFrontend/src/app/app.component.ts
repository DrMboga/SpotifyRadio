import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavButtonComponent } from './components/nav-button/nav-button.component';
import { MatToolbar } from '@angular/material/toolbar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavButtonComponent, MatToolbar],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  title = 'RadioFrontend';
}
