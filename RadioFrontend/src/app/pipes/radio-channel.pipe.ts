import { Pipe, PipeTransform } from '@angular/core';
import { RadioChannel } from '../model/radio-channel';

@Pipe({
  name: 'radioChannel',
})
export class RadioChannelPipe implements PipeTransform {
  /**
   * Finds a channel in the list by frequency
   * @param channel
   * @param channelsList
   */
  transform(channel: number, channelsList: RadioChannel[]): RadioChannel | undefined {
    return channelsList.find(c => c.sabaFrequency === channel);
  }
}
