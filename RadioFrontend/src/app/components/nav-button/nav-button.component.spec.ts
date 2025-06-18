import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NavButtonComponent } from './nav-button.component';
import { RouterModule } from '@angular/router';

describe('NavButtonComponent', () => {
  let component: NavButtonComponent;
  let fixture: ComponentFixture<NavButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavButtonComponent, RouterModule.forRoot([])],
    }).compileComponents();

    fixture = TestBed.createComponent(NavButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
