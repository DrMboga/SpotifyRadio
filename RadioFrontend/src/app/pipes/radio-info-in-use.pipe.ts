import { Pipe, PipeTransform } from '@angular/core';
import { RadioStationInfo } from '../model/radio-station-info';
import { RadioChannel } from '../model/radio-channel';

@Pipe({
  name: 'radioInfoInUse',
})
export class RadioInfoInUsePipe implements PipeTransform {
  transform(radioInfo: RadioStationInfo, channelsList: RadioChannel[]): unknown {
    return channelsList.find(channel => radioInfo.name === channel.name);
  }
}
