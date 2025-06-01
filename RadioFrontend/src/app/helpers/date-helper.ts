export const currentDateInSeconds = (): number => new Date().getTime() / 1000;

export const utcEpochToDate = (epoch: number): Date => new Date(epoch * 1000);
