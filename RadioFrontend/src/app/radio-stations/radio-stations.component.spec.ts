import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RadioStationsComponent } from './radio-stations.component';
import { RadioStore } from '../store/radio.store';

describe('RadioStationsComponent', () => {
  let component: RadioStationsComponent;
  let fixture: ComponentFixture<RadioStationsComponent>;
  let radioStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RadioStationsComponent],
      providers: [{ provide: RadioStore, useValue: {} }],
    }).compileComponents();

    radioStore = TestBed.inject(RadioStore);
    // https://ngrx.io/guide/signals/signal-store/testing#mocking-the-signalstore

    fixture = TestBed.createComponent(RadioStationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
