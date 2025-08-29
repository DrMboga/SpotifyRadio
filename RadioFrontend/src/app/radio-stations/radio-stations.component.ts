import { Component, effect, inject, signal } from '@angular/core';
import { RadioStore } from '../store/radio.store';
import { MatRadioModule } from '@angular/material/radio';
import { FormsModule } from '@angular/forms';
import { MatDividerModule } from '@angular/material/divider';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatOption, MatSelect } from '@angular/material/select';
import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';
import { SabaRadioChannelInfoComponent } from '../components/saba-radio-channel-info/saba-radio-channel-info.component';
import { RadioChannelPipe } from '../pipes/radio-channel.pipe';
import { RadioInfoInUsePipe } from '../pipes/radio-info-in-use.pipe';

@Component({
  selector: 'app-radio-stations',
  imports: [
    MatRadioModule,
    FormsModule,
    MatDividerModule,
    MatButton,
    MatSelect,
    MatOption,
    MatFormField,
    CdkDropList,
    CdkDrag,
    SabaRadioChannelInfoComponent,
    RadioChannelPipe,
    RadioInfoInUsePipe,
  ],
  templateUrl: './radio-stations.component.html',
  styleUrl: './radio-stations.component.css',
})
export class RadioStationsComponent {
  private readonly radioStore = inject(RadioStore);

  buttons = this.radioStore.radioButtonsList;
  sabaStationsFrequenciesList = this.radioStore.sabaStationsList;
  sabaRadioChannels = this.radioStore.sabaRadioChannels;

  selectedButton = signal<number>(2);

  constructor() {
    effect(() => {
      const button = this.selectedButton();
      this.radioStore.getSabaRadioChannels(button);
    });
  }

  radioStationDrop(event: CdkDragDrop<string>) {
    const sameContainer = event.previousContainer === event.container;

    if (!sameContainer) {
      const channel = this.getSabaChannelByIndex(event.previousIndex);
      // Moved station from SABA Channels to the stations list means that channel should be free
      this.radioStore.deleteSabaRadioChannel({
        button: this.selectedButton(),
        sabaFrequency: channel,
      });
    }
  }

  sabaChannelDrop(event: CdkDragDrop<RadioStationInfo>) {
    const sameContainer = event.previousContainer === event.container;
    if (!sameContainer) {
      const radioStation = event.item.data as RadioStationInfo;
      const channel = this.getSabaChannelByIndex(event.currentIndex);
      // Attach a station to SABA channel
      this.attachRadioChannel(channel, radioStation);
    }
    if (sameContainer) {
      // Reattach station from one SABA Channel to another (move a ro to other position)
      const previousChannel = this.getSabaChannelByIndex(event.previousIndex);
      const currentChannel = this.getSabaChannelByIndex(event.currentIndex);
      const station = this.sabaRadioChannels().find(b => b.sabaFrequency === previousChannel);
      if (station) {
        // Delete previous station
        this.radioStore.deleteSabaRadioChannel({
          button: this.selectedButton(),
          sabaFrequency: previousChannel,
        });
        // Attach new channel
        station.sabaFrequency = currentChannel;
        this.radioStore.setSabaRadioChannel(station);
      }
    }
  }

  saveRadioUrl(radioChannel: RadioChannel) {
    this.radioStore.setSabaRadioChannel(radioChannel);
  }

  private getSabaChannelByIndex(index: number) {
    if (this.sabaStationsFrequenciesList().length > index) {
      return this.sabaStationsFrequenciesList()[index];
    }
    return -1;
  }

  private attachRadioChannel(sabaFrequency: number, radio: RadioStationInfo) {
    const channel: RadioChannel = {
      sabaFrequency,
      name: radio.name,
      button: this.selectedButton(),
      region: radio.region,
      streamUrl: radio.stationStreamUrl,
    };
    this.radioStore.setSabaRadioChannel(channel);
  }
}
