import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SabaRadioChannelInfoComponent } from './saba-radio-channel-info.component';

describe('SabaRadioChannelInfoComponent', () => {
  let component: SabaRadioChannelInfoComponent;
  let fixture: ComponentFixture<SabaRadioChannelInfoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SabaRadioChannelInfoComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SabaRadioChannelInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // should show anchor if streamURL is filled

  // should show input and button when streamURL is empty

  // should call saveUrl method on button click and stream URL input is not empty

  // should not call saveUrl method on button click and stream URL input is empty
});
