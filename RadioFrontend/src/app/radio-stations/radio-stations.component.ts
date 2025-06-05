import { Component, computed, effect, inject, signal } from '@angular/core';
import { RadioStore } from '../store/radio.store';
import { MatRadioModule } from '@angular/material/radio';
import { FormsModule } from '@angular/forms';
import { MatDividerModule } from '@angular/material/divider';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatOption, MatSelect } from '@angular/material/select';

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
}
