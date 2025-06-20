import { Pipe, PipeTransform } from '@angular/core';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';

@Pipe({
  name: 'radioInfoInUse',
})
export class RadioInfoInUsePipe implements PipeTransform {
  /**
   * Finds a channel in the list by channel name
   * @param radioInfo
   * @param channelsList
   */
  transform(radioInfo: RadioStationInfo, channelsList: RadioChannel[]): RadioChannel | undefined {
    return channelsList.find(channel => radioInfo.name === channel.name);
  }
}
