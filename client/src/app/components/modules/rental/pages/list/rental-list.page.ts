import { AsyncPipe, CurrencyPipe, DatePipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

import { combineLatest, filter, map, switchMap, take } from 'rxjs';

import { RentalDetailModel, RentalPlanTypeModel } from '../../models/rental.models';
import { RentalService } from '../../services/rental.service';
import { DriverDetailModel } from '../../../driver/models/driver.models';
import { DriverService } from '../../../driver/services/driver.service';
import { VehicleDetailModel } from '../../../vehicle/models/vehicle.models';
import { VehicleService } from '../../../vehicle/services/vehicle.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { MatDialog } from '@angular/material/dialog';
import { RentalSendReceiptEmailDialogComponent } from './dialogs/rental-send-receipt-email.dialog';
import { MatChip } from '@angular/material/chips';

export interface RentalListItemViewModel extends RentalDetailModel {
  driverName: string;
  vehicleLabel: string;
  planTypeLabel: string;
  startDateFormatted: string;
  expectedReturnDateFormatted: string;
  estimatedAmountFormatted: string;
}

@Component({
  selector: 'app-rental-list',
  standalone: true,
  imports: [
    AsyncPipe,
    DatePipe,
    CurrencyPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    SlicePipe,
  ],
  templateUrl: './rental-list.page.html',
})
export class RentalListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly rentalService = inject(RentalService);
  private readonly driverService = inject(DriverService);
  private readonly vehicleService = inject(VehicleService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  private readonly planTypeLabels: Record<RentalPlanTypeModel, string> = {
    [RentalPlanTypeModel.Daily]: 'Plano Diário',
    [RentalPlanTypeModel.Controlled]: 'Plano Controlado',
    [RentalPlanTypeModel.Free]: 'Plano Livre',
  };

  private readonly rentalsSource$ = this.route.data.pipe(
    map((data) => (data['rentals'] as RentalDetailModel[]) ?? []),
  );

  private readonly drivers$ = this.driverService
    .getAllDrivers()
    .pipe(map((drivers) => drivers ?? []));

  private readonly vehicles$ = this.vehicleService
    .getAllVehicles()
    .pipe(map((vehicles) => vehicles ?? []));

  protected readonly rentals$ = combineLatest([
    this.rentalsSource$,
    this.drivers$,
    this.vehicles$,
  ]).pipe(map(([rentals, drivers, vehicles]) => this.mapToViewModel(rentals, drivers, vehicles)));

  private mapToViewModel(
    rentals: RentalDetailModel[],
    drivers: DriverDetailModel[],
    vehicles: VehicleDetailModel[],
  ): RentalListItemViewModel[] {
    return rentals.map<RentalListItemViewModel>((rental) => {
      const driver = drivers.find((d) => d.id === rental.driverId);
      const vehicle = vehicles.find((v) => v.id === rental.vehicleId);

      const driverName: string = driver?.name ?? rental.driverId;
      const vehicleLabel: string = vehicle
        ? `${vehicle.brand} ${vehicle.model} (${vehicle.licensePlate})`
        : rental.vehicleId;

      const planTypeLabel: string = this.planTypeLabels[rental.planType] ?? rental.planType;

      const startDateFormatted: string = rental.startDate;
      const expectedReturnDateFormatted: string = rental.expectedReturnDate;

      const estimatedAmountFormatted: string = rental.estimatedRentalAmount.toFixed(2);

      return {
        ...rental,
        driverName,
        vehicleLabel,
        planTypeLabel,
        startDateFormatted,
        expectedReturnDateFormatted,
        estimatedAmountFormatted,
      };
    });
  }

  protected downloadReceiptPdf(rentalId: string): void {
    this.rentalService
      .getReceiptPdf(rentalId)
      .pipe(take(1))
      .subscribe({
        next: (response) => {
          const pdfBlob: Blob | null = response.body;
          if (!pdfBlob) return;

          const contentDispositionHeader: string | null =
            response.headers.get('content-disposition');

          const fileName: string =
            this.tryGetFileNameFromContentDisposition(contentDispositionHeader) ??
            `Recibo_Aluguel_${rentalId}.pdf`;

          const objectUrl: string = URL.createObjectURL(pdfBlob);

          const anchorElement: HTMLAnchorElement = document.createElement('a');
          anchorElement.href = objectUrl;
          anchorElement.download = fileName;
          anchorElement.click();

          URL.revokeObjectURL(objectUrl);
        },
        error: () => {
          this.notificationService.error('Failed to download rental receipt PDF.');
        },
      });
  }

  private tryGetFileNameFromContentDisposition(
    contentDispositionHeader: string | null,
  ): string | null {
    if (!contentDispositionHeader) return null;

    const fileNameMatch: RegExpMatchArray | null =
      contentDispositionHeader.match(/filename="([^"]+)"/i);

    if (fileNameMatch?.[1]) return fileNameMatch[1];

    const fileNameStarMatch: RegExpMatchArray | null =
      contentDispositionHeader.match(/filename\*=UTF-8''([^;]+)/i);

    if (fileNameStarMatch?.[1]) return decodeURIComponent(fileNameStarMatch[1]);

    return null;
  }

  protected openReceiptPdf(rentalId: string): void {
    this.rentalService
      .getReceiptPdf(rentalId)
      .pipe(take(1))
      .subscribe({
        next: (response) => {
          const pdfBlob: Blob | null = response.body;
          if (!pdfBlob) return;

          const objectUrl: string = URL.createObjectURL(pdfBlob);

          window.open(objectUrl, '_blank', 'noopener');
        },
        error: () => console.error('Failed to open rental receipt PDF.'),
      });
  }

  protected openSendReceiptEmailDialog(rentalId: string): void {
    const dialogRef = this.dialog.open(RentalSendReceiptEmailDialogComponent, {
      width: '420px',
      panelClass: 'as-shell-dialog-centered',
      data: { defaultEmail: '' },
    });

    dialogRef
      .afterClosed()
      .pipe(
        take(1),
        filter((result): result is { email: string } => !!result?.email),
        switchMap((result) =>
          this.rentalService.sendReceiptByEmail(rentalId, { email: result.email }),
        ),
      )
      .subscribe({
        next: () => {
          this.notificationService.success('Recibo enviado por e-mail com sucesso.');
        },
        error: (err) => {
          const statusCode: number | undefined = err?.status;

          if (statusCode === 429) {
            this.notificationService.error(
              'Limite de envios atingido. Tente novamente em instantes.',
            );
            return;
          }

          this.notificationService.error('Falha ao enviar recibo por e-mail.');
        },
      });
  }
}
