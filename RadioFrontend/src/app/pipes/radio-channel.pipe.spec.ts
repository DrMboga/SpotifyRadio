import { RadioChannelPipe } from './radio-channel.pipe';
import { MOCK_RADIO_CHANNELS_LIST } from '../mock/radio-mock';

describe('RadioChannelPipe', () => {
  it('create an instance', () => {
    const pipe = new RadioChannelPipe();
    expect(pipe).toBeTruthy();
  });

  it.each<number, string>([
    [89, 'Channel 2'],
    [91, 'Channel 4'],
  ])('should find appropriate channels', (frequency: number, expectedChannelName: string) => {
    const pipe = new RadioChannelPipe();
    const result = pipe.transform(frequency, MOCK_RADIO_CHANNELS_LIST);

    expect(result).toBeTruthy();
    expect(result?.name).toEqual(expectedChannelName);
  });
});
