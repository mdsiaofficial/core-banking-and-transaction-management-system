/**
 * Precision financial utilities for BDT currency handling without floating-point errors.
 * Scale: 4 decimal places internally (1 unit = 10,000 sub-units / Paisa fractions).
 */

const SCALE_FACTOR = 10000n;

export class MoneyUtil {
  /**
   * Converts a decimal string or number to scaled BigInt.
   * "1250.50" -> 12505000n
   */
  public static toScaledBigInt(amount: string | number | null | undefined): bigint {
    if (amount === null || amount === undefined || amount === '') {
      return 0n;
    }
    const cleanStr = String(amount).trim();
    if (!cleanStr) return 0n;

    const isNegative = cleanStr.startsWith('-');
    const absStr = isNegative ? cleanStr.substring(1) : cleanStr;

    const parts = absStr.split('.');
    const integerPart = BigInt(parts[0] || '0');
    let fractionalStr = parts[1] || '';

    // Pad or trim fractional to 4 digits
    if (fractionalStr.length > 4) {
      fractionalStr = fractionalStr.substring(0, 4);
    } else {
      fractionalStr = fractionalStr.padEnd(4, '0');
    }

    const fractionalPart = BigInt(fractionalStr);
    const scaled = integerPart * SCALE_FACTOR + fractionalPart;
    return isNegative ? -scaled : scaled;
  }

  /**
   * Converts scaled BigInt back to a standard decimal string (e.g., "1250.5000").
   */
  public static fromScaledBigInt(scaled: bigint, decimals: number = 2): string {
    const isNegative = scaled < 0n;
    const absScaled = isNegative ? -scaled : scaled;

    const integerPart = absScaled / SCALE_FACTOR;
    const fractionalPart = absScaled % SCALE_FACTOR;

    const fracStr = fractionalPart.toString().padStart(4, '0');
    const trimmedFrac = decimals <= 2 ? fracStr.substring(0, 2) : fracStr.substring(0, decimals);

    const result = `${integerPart}.${trimmedFrac}`;
    return isNegative ? `-${result}` : result;
  }

  /**
   * Compares two monetary amounts.
   * Returns:
   *  1 if a > b
   *  -1 if a < b
   *  0 if a == b
   */
  public static compare(a: string | number, b: string | number): number {
    const bigA = this.toScaledBigInt(a);
    const bigB = this.toScaledBigInt(b);
    if (bigA > bigB) return 1;
    if (bigA < bigB) return -1;
    return 0;
  }

  public static isGreaterThan(a: string | number, b: string | number): boolean {
    return this.compare(a, b) > 0;
  }

  public static isLessThan(a: string | number, b: string | number): boolean {
    return this.compare(a, b) < 0;
  }

  public static isEqual(a: string | number, b: string | number): boolean {
    return this.compare(a, b) === 0;
  }

  public static add(a: string | number, b: string | number): string {
    const sum = this.toScaledBigInt(a) + this.toScaledBigInt(b);
    return this.fromScaledBigInt(sum, 4);
  }

  public static subtract(a: string | number, b: string | number): string {
    const diff = this.toScaledBigInt(a) - this.toScaledBigInt(b);
    return this.fromScaledBigInt(diff, 4);
  }

  /**
   * Formats a monetary string or number to BDT currency representation.
   * e.g., "15000.50" -> "৳ 15,000.50"
   */
  public static formatBDT(
    amount: string | number | null | undefined,
    options?: { showCurrency?: boolean; symbol?: string; minDecimals?: number; maxDecimals?: number }
  ): string {
    if (amount === null || amount === undefined || amount === '') {
      return '৳ 0.00';
    }

    const scaled = this.toScaledBigInt(amount);
    const isNegative = scaled < 0n;
    const absScaled = isNegative ? -scaled : scaled;

    const integerPart = absScaled / SCALE_FACTOR;
    const fractionalPart = absScaled % SCALE_FACTOR;

    const minDec = options?.minDecimals ?? 2;
    const maxDec = options?.maxDecimals ?? 2;

    const fracStr = fractionalPart.toString().padStart(4, '0').substring(0, maxDec);

    // Format integer part using Intl.NumberFormat
    const formattedInteger = new Intl.NumberFormat('en-BD', {
      useGrouping: true,
      maximumFractionDigits: 0,
    }).format(integerPart);

    const symbol = options?.symbol ?? '৳';
    const showCurrency = options?.showCurrency ?? true;

    const sign = isNegative ? '-' : '';
    const formattedNumber = `${formattedInteger}.${fracStr}`;

    return showCurrency ? `${sign}${symbol} ${formattedNumber}` : `${sign}${formattedNumber}`;
  }

  /**
   * Validates a 13-digit account number using the standard Luhn checksum algorithm.
   */
  public static isValidAccountNumber(accountNumber: string): boolean {
    if (!accountNumber) return false;
    const cleaned = accountNumber.replace(/\D/g, '');
    if (cleaned.length < 10 || cleaned.length > 16) return false;

    let sum = 0;
    let shouldDouble = false;

    for (let i = cleaned.length - 1; i >= 0; i--) {
      let digit = parseInt(cleaned.charAt(i), 10);
      if (shouldDouble) {
        digit *= 2;
        if (digit > 9) digit -= 9;
      }
      sum += digit;
      shouldDouble = !shouldDouble;
    }

    return sum % 10 === 0;
  }
}
