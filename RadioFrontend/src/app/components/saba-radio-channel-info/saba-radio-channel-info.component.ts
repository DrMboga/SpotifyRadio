import { Component, input } from '@angular/core';
import { RadioChannel } from '../../model/radio-channel';

@Component({
  selector: 'app-saba-radio-channel-info',
  imports: [],
  templateUrl: './saba-radio-channel-info.component.html',
  styleUrl: './saba-radio-channel-info.component.css',
})
export class SabaRadioChannelInfoComponent {
  radioChannel = input<RadioChannel>();
}
