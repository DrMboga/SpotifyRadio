import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpotifyLoginCodeComponent } from './spotify-login-code.component';

describe('SpotifyLoginCodeComponent', () => {
  let component: SpotifyLoginCodeComponent;
  let fixture: ComponentFixture<SpotifyLoginCodeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpotifyLoginCodeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SpotifyLoginCodeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
