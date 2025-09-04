import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';

import { SabaRadioChannelInfoComponent } from './saba-radio-channel-info.component';
import { ComponentRef } from '@angular/core';
import { RadioChannel } from '../../model/radio-channel';

describe('SabaRadioChannelInfoComponent', () => {
  let component: SabaRadioChannelInfoComponent;
  let componentRef: ComponentRef<SabaRadioChannelInfoComponent>;
  let fixture: ComponentFixture<SabaRadioChannelInfoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SabaRadioChannelInfoComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SabaRadioChannelInfoComponent);
    componentRef = fixture.componentRef;
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show anchor if streamURL is filled', fakeAsync(() => {
    // Arrange
    const radioChannel: RadioChannel = {
      name: 'fake Radio',
      streamUrl: 'http://fake-radio.com',
      stationDetailsUrl: 'http://fake-radio.com',
      sabaFrequency: 89,
      button: 1,
    };

    componentRef.setInput('radioChannel', radioChannel);
    tick();

    // Act
    fixture.detectChanges();

    // Assert
    const anchor = fixture.nativeElement.querySelector('a');
    expect(anchor).toBeTruthy();
    expect(anchor.textContent).toBe('Stream');
    expect(anchor.getAttribute('href')).toBe(radioChannel.streamUrl);

    const input = fixture.nativeElement.querySelector('input');
    expect(input).toBeFalsy();
    const button = fixture.nativeElement.querySelector('button');
    expect(button).toBeFalsy();
  }));

  it('should show input and button when streamURL is empty', fakeAsync(() => {
    // Arrange
    const radioChannel: RadioChannel = {
      name: 'fake Radio',
      stationDetailsUrl: 'http://fake-radio.com',
      sabaFrequency: 89,
      button: 1,
    };

    componentRef.setInput('radioChannel', radioChannel);
    tick();

    // Act
    fixture.detectChanges();

    // Assert
    const anchor = fixture.nativeElement.querySelector('a');
    expect(anchor).toBeFalsy();
    const input = fixture.nativeElement.querySelector('input');
    expect(input).toBeTruthy();
    const button = fixture.nativeElement.querySelector('button');
    expect(button).toBeTruthy();
  }));

  it('should emit savedRadioChannel output signal on button click and stream URL input is not empty', fakeAsync(() => {
    // Arrange
    const streamUrl = 'http://fake-radio.com';

    const outputEventSpy = jest.spyOn(component.savedRadioChannel, 'emit');

    const radioChannel: RadioChannel = {
      name: 'fake Radio',
      stationDetailsUrl: 'http://fake-radio.com',
      sabaFrequency: 89,
      button: 1,
    };
    componentRef.setInput('radioChannel', radioChannel);
    tick();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    const input = fixture.nativeElement.querySelector('input');
    input.value = streamUrl;
    input.dispatchEvent(new Event('input'));
    tick();
    fixture.detectChanges();

    // Act
    button.click();
    tick();
    fixture.detectChanges();

    // Assert
    expect(outputEventSpy).toHaveBeenCalledWith({
      name: 'fake Radio',
      stationDetailsUrl: 'http://fake-radio.com',
      sabaFrequency: 89,
      button: 1,
      streamUrl,
    });
  }));

  it('should not emit savedRadioChannel output signal on button click and stream URL input is empty', fakeAsync(() => {
    // Arrange
    const outputEventSpy = jest.spyOn(component.savedRadioChannel, 'emit');

    const radioChannel: RadioChannel = {
      name: 'fake Radio',
      stationDetailsUrl: 'http://fake-radio.com',
      sabaFrequency: 89,
      button: 1,
    };
    componentRef.setInput('radioChannel', radioChannel);
    tick();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');

    // Act
    button.click();
    tick();
    fixture.detectChanges();

    // Assert
    expect(outputEventSpy).not.toHaveBeenCalled();
  }));
});
