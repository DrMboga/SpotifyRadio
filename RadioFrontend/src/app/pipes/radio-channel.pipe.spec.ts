import { RadioChannelPipe } from './radio-channel.pipe';
import { RadioChannel } from '../model/radio-channel';

describe('RadioChannelPipe', () => {
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
    const pipe = new RadioChannelPipe();
    expect(pipe).toBeTruthy();
  });

  it.each<number, string>([
    [89, 'Channel 2'],
    [91, 'Channel 4'],
  ])('should find appropriate channels', (frequency: number, expectedChannelName: string) => {
    const pipe = new RadioChannelPipe();
    const result = pipe.transform(frequency, channelsList);

    expect(result).toBeTruthy();
    expect(result?.name).toEqual(expectedChannelName);
  });
});
