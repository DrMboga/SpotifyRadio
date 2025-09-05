import { Component, input } from '@angular/core';
import { RadioStationInfo } from '../../model/radio-station-info';
import { MatIcon } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { TextSplitPipe } from '../../pipes/text-split.pipe';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-radio-station-info',
  imports: [MatIcon, MatButtonModule, MatMenuModule, TextSplitPipe, MatChipsModule],
  templateUrl: './radio-station-info.component.html',
  styleUrl: './radio-station-info.component.css',
})
export class RadioStationInfoComponent {
  station = input<RadioStationInfo>();
}
