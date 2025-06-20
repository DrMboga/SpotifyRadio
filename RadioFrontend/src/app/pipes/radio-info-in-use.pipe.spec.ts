import { RadioInfoInUsePipe } from './radio-info-in-use.pipe';
import { RadioChannel } from '../model/radio-channel';
import { RadioStationInfo } from '../model/radio-station-info';

describe('RadioInfoInUsePipe', () => {
  const channelsList: RadioChannel[] = [
    {
      button: 1,
      region: 'Bayern',
      name: 'Channel 1',
      sabaFrequency: 88,
    },
    {
      button: 1,
      region: 'Bayern',
      name: 'Channel 2',
      sabaFrequency: 89,
    },
    {
      button: 1,
      region: 'Bayern',
      name: 'Channel 3',
      sabaFrequency: 90,
    },
    {
      button: 1,
      region: 'Bayern',
      name: 'Channel 4',
      sabaFrequency: 91,
    },
  ];

  it('create an instance', () => {
    const pipe = new RadioInfoInUsePipe();
    expect(pipe).toBeTruthy();
  });

  it.each<string>(['Channel 2', 'Channel 4'])(
    'should find appropriate channels',
    (expectedChannelName: string) => {
      const pipe = new RadioInfoInUsePipe();
      const radioInfo: RadioStationInfo = {
        name: expectedChannelName,
        region: 'Bayern',
        frequency: 87.5,
        detailsUrl: '',
        stationUrl: '',
        stationImageUrl: '',
      };
      const result = pipe.transform(radioInfo, channelsList);

      expect(result).toBeTruthy();
      expect(result?.name).toEqual(expectedChannelName);
    },
  );
});
