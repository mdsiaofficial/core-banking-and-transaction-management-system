import { MoneyPipe } from './money.pipe';

describe('MoneyPipe', () => {
  let pipe: MoneyPipe;

  beforeEach(() => {
    pipe = new MoneyPipe();
  });

  it('should transform numeric string to BDT currency string', () => {
    const res = pipe.transform('1500.50');
    expect(res).toContain('1,500.50');
    expect(res).toContain('৳');
  });

  it('should transform integer to formatted BDT currency', () => {
    const res = pipe.transform(25000);
    expect(res).toContain('25,000.00');
  });

  it('should format null or undefined gracefully', () => {
    expect(pipe.transform(null)).toContain('0.00');
    expect(pipe.transform(undefined)).toContain('0.00');
  });

  it('should allow hiding the currency symbol', () => {
    const res = pipe.transform('100.00', false);
    expect(res).toBe('100.00');
  });
});
