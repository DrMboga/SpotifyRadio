import { Component, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { Router, RouterLink } from '@angular/router';
import { MatAnchor } from '@angular/material/button';

@Component({
  selector: 'app-nav-button',
  imports: [CommonModule, MatIconModule, MatAnchor, RouterLink],
  templateUrl: './nav-button.component.html',
  styleUrl: './nav-button.component.css',
})
export class NavButtonComponent {
  routerLink = input<string>();
  icon = input<string>();
  label = input<string>();

  public readonly router = inject(Router);
}
