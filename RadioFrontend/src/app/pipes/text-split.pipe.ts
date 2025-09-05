import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'textSplit',
})
export class TextSplitPipe implements PipeTransform {
  transform(text?: string, splitter = '|'): string[] {
    return text === undefined || text === '' ? [] : text.split(splitter);
  }
}
