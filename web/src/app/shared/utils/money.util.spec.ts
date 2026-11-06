import { MoneyUtil } from './money.util';

describe('MoneyUtil', () => {
  describe('toScaledBigInt & fromScaledBigInt', () => {
    it('should convert standard decimal string to scaled BigInt with 4 decimals', () => {
      const scaled = MoneyUtil.toScaledBigInt('1250.50');
      expect(scaled).toBe(12505000n);
    });

    it('should accurately handle fractional values without floating point drift', () => {
      // In JS float: 0.1 + 0.2 = 0.30000000000000004
      const sum = MoneyUtil.add('0.10', '0.20');
      expect(sum).toBe('0.3000');
    });

    it('should handle zero and empty inputs gracefully', () => {
      expect(MoneyUtil.toScaledBigInt('')).toBe(0n);
      expect(MoneyUtil.toScaledBigInt(null)).toBe(0n);
      expect(MoneyUtil.toScaledBigInt(0)).toBe(0n);
    });

    it('should handle negative numbers correctly', () => {
      const scaled = MoneyUtil.toScaledBigInt('-500.25');
      expect(scaled).toBe(-5002500n);
      expect(MoneyUtil.fromScaledBigInt(scaled)).toBe('-500.25');
    });
  });

  describe('compare, isGreaterThan, isLessThan, isEqual', () => {
    it('should correctly order monetary values', () => {
      expect(MoneyUtil.compare('100.50', '50.25')).toBe(1);
      expect(MoneyUtil.compare('50.25', '100.50')).toBe(-1);
      expect(MoneyUtil.compare('100.50', '100.50')).toBe(0);

      expect(MoneyUtil.isGreaterThan('100.01', '100.00')).toBe(true);
      expect(MoneyUtil.isLessThan('99.99', '100.00')).toBe(true);
      expect(MoneyUtil.isEqual('500.00', '500.0000')).toBe(true);
    });
  });

  describe('arithmetic', () => {
    it('should add amounts precisely', () => {
      expect(MoneyUtil.add('1000.50', '250.25')).toBe('1250.7500');
    });

    it('should subtract amounts precisely', () => {
      expect(MoneyUtil.subtract('1000.00', '250.50')).toBe('749.5000');
    });
  });

  describe('formatBDT', () => {
    it('should format BDT currency with symbol and comma grouping', () => {
      const formatted = MoneyUtil.formatBDT('1250000.50');
      expect(formatted).toContain('1,250,000.50');
      expect(formatted).toContain('৳');
    });

    it('should format 0 as ৳ 0.00', () => {
      expect(MoneyUtil.formatBDT('0')).toContain('0.00');
      expect(MoneyUtil.formatBDT(null)).toContain('0.00');
    });
  });

  describe('isValidAccountNumber (Luhn algorithm)', () => {
    it('should validate valid Luhn account numbers', () => {
      // 79927398713 is a known valid Luhn number
      expect(MoneyUtil.isValidAccountNumber('79927398713')).toBe(true);
    });

    it('should reject invalid Luhn account numbers', () => {
      expect(MoneyUtil.isValidAccountNumber('79927398714')).toBe(false);
      expect(MoneyUtil.isValidAccountNumber('123')).toBe(false);
      expect(MoneyUtil.isValidAccountNumber('')).toBe(false);
    });
  });
});
