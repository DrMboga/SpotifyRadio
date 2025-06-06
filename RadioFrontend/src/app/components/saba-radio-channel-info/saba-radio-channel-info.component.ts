import { Component, input, output, signal } from '@angular/core';
import { RadioChannel } from '../../model/radio-channel';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatInput, MatLabel } from '@angular/material/input';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-saba-radio-channel-info',
  imports: [MatButton, MatInput, FormsModule, MatFormField, MatLabel],
  templateUrl: './saba-radio-channel-info.component.html',
  styleUrl: './saba-radio-channel-info.component.css',
})
export class SabaRadioChannelInfoComponent {
  radioChannel = input<RadioChannel>();
  streamUrl = signal<string | undefined>(undefined);

  savedRadioChannel = output<RadioChannel>();

  save(): void {
    if (this.radioChannel() && this.streamUrl()) {
      const channel = this.radioChannel()!;
      channel.streamUrl = this.streamUrl();
      this.savedRadioChannel.emit(channel);
    }
  }
}
