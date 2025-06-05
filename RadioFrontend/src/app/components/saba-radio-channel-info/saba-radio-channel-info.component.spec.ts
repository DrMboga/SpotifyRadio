import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SabaRadioChannelInfoComponent } from './saba-radio-channel-info.component';

describe('SabaRadioChannelInfoComponent', () => {
  let component: SabaRadioChannelInfoComponent;
  let fixture: ComponentFixture<SabaRadioChannelInfoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SabaRadioChannelInfoComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SabaRadioChannelInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
