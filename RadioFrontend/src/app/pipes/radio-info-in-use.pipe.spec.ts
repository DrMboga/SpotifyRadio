import { RadioInfoInUsePipe } from './radio-info-in-use.pipe';
import { RadioStationInfo } from '../model/radio-station-info';
import { MOCK_RADIO_CHANNELS_LIST } from '../mock/radio-mock';

describe('RadioInfoInUsePipe', () => {
  it('create an instance', () => {
    const pipe = new RadioInfoInUsePipe();
    expect(pipe).toBeTruthy();
  });

  it.each<string>(['Channel 2', 'Channel 4'])(
    'should find appropriate channels',
    (expectedChannelName: string) => {
      const pipe = new RadioInfoInUsePipe();
      const detailsUrl = expectedChannelName === 'Channel 2' ? '/radio/g2/' : '/radio/g4/';
      const radioInfo: RadioStationInfo = {
        name: expectedChannelName,
        detailsUrl,
        stationImageUrl: '',
        country: 'fake',
        stationProcessed: true,
      };
      const result = pipe.transform(radioInfo, MOCK_RADIO_CHANNELS_LIST);

      expect(result).toBeTruthy();
      expect(result?.name).toEqual(expectedChannelName);
    },
  );
});
