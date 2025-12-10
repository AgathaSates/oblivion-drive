import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'phoneNumberFormat',
  standalone: true,
})
export class PhoneNumberFormatPipe implements PipeTransform {
  public transform(rawPhoneNumber: string | null | undefined): string {
    if (!rawPhoneNumber) {
      return '';
    }

    const digitsOnly: string = rawPhoneNumber.replace(/\D/g, '');

    if (digitsOnly.length === 11) {
      const areaCode: string = digitsOnly.slice(0, 2);
      const firstPart: string = digitsOnly.slice(2, 7);
      const secondPart: string = digitsOnly.slice(7);
      return `(${areaCode}) ${firstPart}-${secondPart}`;
    }

    if (digitsOnly.length === 10) {
      const areaCode: string = digitsOnly.slice(0, 2);
      const firstPart: string = digitsOnly.slice(2, 6);
      const secondPart: string = digitsOnly.slice(6);
      return `(${areaCode}) ${firstPart}-${secondPart}`;
    }

    return rawPhoneNumber;
  }
}
