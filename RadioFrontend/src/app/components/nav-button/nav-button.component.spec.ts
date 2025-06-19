import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NavButtonComponent } from './nav-button.component';
import { RouterModule } from '@angular/router';
import { ComponentRef } from '@angular/core';

describe('NavButtonComponent', () => {
  const route = 'home';

  let component: NavButtonComponent;
  let componentRef: ComponentRef<NavButtonComponent>;
  let fixture: ComponentFixture<NavButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavButtonComponent, RouterModule.forRoot([])],
    }).compileComponents();

    fixture = TestBed.createComponent(NavButtonComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
    componentRef.setInput('routerLink', route);
    componentRef.setInput('icon', 'home');
    componentRef.setInput('label', 'Hi there');

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show normal mat-button when route is not matched', () => {
    const button = fixture.nativeElement.querySelector('a');
    expect(button.classList).toContain('mat-mdc-button');
    expect(button.classList).not.toContain('mat-mdc-outlined-button');
  });
});
