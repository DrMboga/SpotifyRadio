import { Component, computed, effect, inject, signal } from '@angular/core';
import { RadioStore } from '../store/radio.store';
import { MatRadioModule } from '@angular/material/radio';
import { FormsModule } from '@angular/forms';
import { MatDividerModule } from '@angular/material/divider';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatOption, MatSelect } from '@angular/material/select';
import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { RadioStationInfo } from '../model/radio-station-info';

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
  ],
  templateUrl: './radio-stations.component.html',
  styleUrl: './radio-stations.component.css',
})
export class RadioStationsComponent {
  private readonly radioStore = inject(RadioStore);

  buttons = this.radioStore.radioButtonsList;
  regionsList = this.radioStore.regionsList;
  buttonRegions = this.radioStore.radioButtonRegions;
  sabaStationsFrequenciesList = this.radioStore.sabaStationsList;
  regionStationsList = this.radioStore.regionStationsList;

  selectedButton = signal<number>(2);
  selectedRegion = signal<string>('');
  currentButtonRegion = computed(() => {
    const button = this.selectedButton();
    return this.buttonRegions().find(b => b.sabaRadioButton === button);
  });

  regionChanged = signal<boolean>(false);

  constructor() {
    effect(() => {
      const button = this.selectedButton();
      const currentButtonRegion = this.currentButtonRegion();
      this.regionChanged.set(false);
      this.selectedRegion.set(currentButtonRegion ? currentButtonRegion.region : '');
    });
    effect(() => {
      const selectedRegion = this.selectedRegion();
      const currentButtonRegion = this.currentButtonRegion();
      if (selectedRegion && selectedRegion != currentButtonRegion?.region) {
        this.regionChanged.set(true);
      }
      this.radioStore.getRadioStationsByRegion(selectedRegion);
    });
  }

  setRadioButtonRegion(): void {
    this.radioStore.setRadioButtonRegion({
      sabaRadioButton: this.selectedButton(),
      region: this.selectedRegion(),
    });
    this.regionChanged.set(false);
    // TODO: Clean button radio stations (In backend)
  }

  radioStationDrop(event: CdkDragDrop<string>) {
    const sameContainer = event.previousContainer === event.container;

    if (!sameContainer) {
      const channel = this.getSabaChannelByIndex(event.previousIndex);
      console.log(`Channel ${channel} has been dropped`);
    }
  }

  sabaChannelDrop(event: CdkDragDrop<RadioStationInfo>) {
    const sameContainer = event.previousContainer === event.container;
    if (!sameContainer) {
      const radioStation = event.item.data as RadioStationInfo;
      const channel = this.getSabaChannelByIndex(event.currentIndex);
      console.log(
        `Station [${radioStation.frequency} | ${radioStation.name}] has been attached to channel ${channel}`,
      );
    }
    if (sameContainer) {
      const previousChannel = this.getSabaChannelByIndex(event.previousIndex);
      const currentChannel = this.getSabaChannelByIndex(event.currentIndex);
      console.log(
        `Station ... has been moved from channel ${previousChannel} to channel ${currentChannel}`,
      );
    }
  }

  private getSabaChannelByIndex(index: number) {
    if (this.sabaStationsFrequenciesList().length > index) {
      return this.sabaStationsFrequenciesList()[index];
    }
    return -1;
  }
}
