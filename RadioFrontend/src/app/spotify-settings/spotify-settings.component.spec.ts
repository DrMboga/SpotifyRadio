import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpotifySettingsComponent } from './spotify-settings.component';

describe('SpotifySettingsComponent', () => {
  let component: SpotifySettingsComponent;
  let fixture: ComponentFixture<SpotifySettingsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpotifySettingsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SpotifySettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
