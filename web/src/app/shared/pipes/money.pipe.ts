import { Pipe, PipeTransform } from '@angular/core';
import { MoneyUtil } from '../utils/money.util';

@Pipe({
  name: 'money',
  standalone: true,
})
export class MoneyPipe implements PipeTransform {
  transform(
    value: string | number | null | undefined,
    showSymbol: boolean = true,
    decimals: number = 2
  ): string {
    return MoneyUtil.formatBDT(value, {
      showCurrency: showSymbol,
      symbol: '৳',
      minDecimals: decimals,
      maxDecimals: decimals,
    });
  }
}
