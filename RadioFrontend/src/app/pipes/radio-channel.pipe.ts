import { Pipe, PipeTransform } from '@angular/core';
import { RadioChannel } from '../model/radio-channel';

@Pipe({
  name: 'radioChannel',
})
export class RadioChannelPipe implements PipeTransform {
  transform(channel: number, channelsList: RadioChannel[]) {
    return channelsList.find(c => c.sabaFrequency === channel);
  }
}
