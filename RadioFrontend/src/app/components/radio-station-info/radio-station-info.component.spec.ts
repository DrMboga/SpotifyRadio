import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RadioStationInfoComponent } from './radio-station-info.component';

describe('RadioStationInfoComponent', () => {
  let component: RadioStationInfoComponent;
  let fixture: ComponentFixture<RadioStationInfoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RadioStationInfoComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RadioStationInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
