import { AsyncPipe, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog } from '@angular/material/dialog';

import { combineLatest, PartialObserver } from 'rxjs';
import { filter, finalize, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { RentalService } from '../../services/rental.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  RentalDetailModel,
  RentalPlanTypeModel,
  CompleteRentalReturnRequestModel,
  CompleteRentalReturnResponseModel,
  RentalReturnConfirmationDialogResult,
} from '../../models/rental.models';

import { DriverService } from '../../../driver/services/driver.service';
import { DriverDetailModel } from '../../../driver/models/driver.models';
import { VehicleDetailModel } from '../../../vehicle/models/vehicle.models';
import { VehicleService } from '../../../vehicle/services/vehicle.service';

import {
  RentalReturnConfirmationDialogComponent,
  RentalReturnConfirmationDialogData,
} from './dialogs/rental-return-confirmation.dialog';

export interface RentalReturnViewModel extends RentalDetailModel {
  driverName: string;
  vehicleLabel: string;
  planTypeLabel: string;
}

@Component({
  selector: 'app-rental-return',
  standalone: true,
  imports: [
    AsyncPipe,
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    CurrencyPipe,
  ],
  templateUrl: './rental-return.page.html',
})
export class RentalReturnPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly rentalService = inject(RentalService);
  private readonly notificationService = inject(NotificationService);
  private readonly driverService = inject(DriverService);
  private readonly vehicleService = inject(VehicleService);
  private readonly dialog = inject(MatDialog);

  protected isSubmitting: boolean = false;

  private readonly planTypeLabels: Record<RentalPlanTypeModel, string> = {
    [RentalPlanTypeModel.Daily]: 'Plano Diário',
    [RentalPlanTypeModel.Controlled]: 'Plano Controlado',
    [RentalPlanTypeModel.Free]: 'Plano Livre',
  };

  protected readonly returnForm: FormGroup = this.formBuilder.group({
    actualReturnDate: [this.getTodayAsDateInput(), [Validators.required]],
    initialOdometerInKm: [null, [Validators.required, Validators.min(0)]],
    currentOdometerInKm: [null, [Validators.required, Validators.min(0)]],
    isFuelTankFullOnReturn: [true, [Validators.required]],
    hasDamage: [false, [Validators.required]],
    couponName: [null],
  });

  private readonly rental$ = this.route.data.pipe(
    filter((data) => !!data['rental']),
    map((data) => data['rental'] as RentalDetailModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  private readonly drivers$ = this.driverService
    .getAllDrivers()
    .pipe(map((drivers) => drivers ?? []));
  private readonly vehicles$ = this.vehicleService
    .getAllVehicles()
    .pipe(map((vehicles) => vehicles ?? []));

  protected readonly rentalView$ = combineLatest([
    this.rental$,
    this.drivers$,
    this.vehicles$,
  ]).pipe(
    map(([rental, drivers, vehicles]) => this.mapToViewModel(rental, drivers, vehicles)),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  get actualReturnDateControl() {
    return this.returnForm.get('actualReturnDate');
  }
  get initialOdometerInKmControl() {
    return this.returnForm.get('initialOdometerInKm');
  }
  get currentOdometerInKmControl() {
    return this.returnForm.get('currentOdometerInKm');
  }
  get isFuelTankFullOnReturnControl() {
    return this.returnForm.get('isFuelTankFullOnReturn');
  }
  get hasDamageControl() {
    return this.returnForm.get('hasDamage');
  }
  get couponNameControl() {
    return this.returnForm.get('couponName');
  }

  private mapToViewModel(
    rental: RentalDetailModel,
    drivers: DriverDetailModel[],
    vehicles: VehicleDetailModel[],
  ): RentalReturnViewModel {
    const matchedDriver: DriverDetailModel | undefined = drivers.find(
      (d) => d.id === rental.driverId,
    );
    const matchedVehicle: VehicleDetailModel | undefined = vehicles.find(
      (v) => v.id === rental.vehicleId,
    );

    const driverName: string = matchedDriver?.name ?? rental.driverId;
    const vehicleLabel: string = matchedVehicle
      ? `${matchedVehicle.brand} ${matchedVehicle.model} (${matchedVehicle.licensePlate})`
      : rental.vehicleId;

    const planTypeLabel: string = this.planTypeLabels[rental.planType] ?? rental.planType;

    return { ...rental, driverName, vehicleLabel, planTypeLabel };
  }

  private getTodayAsDateInput(): string {
    const today: Date = new Date();
    const year: number = today.getFullYear();
    const month: string = `${today.getMonth() + 1}`.padStart(2, '0');
    const day: string = `${today.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private buildReturnRequestFromForm(): CompleteRentalReturnRequestModel {
    const formValue = this.returnForm.value as {
      actualReturnDate: string;
      initialOdometerInKm: number | string | null;
      currentOdometerInKm: number | string | null;
      isFuelTankFullOnReturn: boolean;
      hasDamage: boolean;
      couponName: string | null;
    };

    const couponText: string = formValue.couponName ? String(formValue.couponName).trim() : '';

    return {
      actualReturnDate: formValue.actualReturnDate,
      initialOdometerInKm: Number(formValue.initialOdometerInKm),
      currentOdometerInKm: Number(formValue.currentOdometerInKm),
      isFuelTankFullOnReturn: Boolean(formValue.isFuelTankFullOnReturn),
      hasDamage: Boolean(formValue.hasDamage),
      couponName: couponText.length > 0 ? couponText : null,
    };
  }

  public requestReturnConfirmation(): void {
    if (this.returnForm.invalid) {
      this.returnForm.markAllAsTouched();
      return;
    }

    if (this.isSubmitting) return;

    const requestModel: CompleteRentalReturnRequestModel = this.buildReturnRequestFromForm();

    this.rentalView$
      .pipe(
        take(1),
        switchMap((rentalView: RentalReturnViewModel) => {
          const dialogData: RentalReturnConfirmationDialogData = {
            rental: rentalView,
            request: requestModel,
          };

          const dialogRef = this.dialog.open<
            RentalReturnConfirmationDialogComponent,
            RentalReturnConfirmationDialogData,
            RentalReturnConfirmationDialogResult
          >(RentalReturnConfirmationDialogComponent, {
            width: '460px',
            panelClass: 'as-shell-dialog-centered',
            data: dialogData,
          });

          return dialogRef.afterClosed().pipe(
            take(1),
            filter((result): result is RentalReturnConfirmationDialogResult => !!result),
            filter((result) => result.confirmed === true),
            switchMap(() => {
              this.isSubmitting = true;
              return this.rentalService.completeRentalReturn(rentalView.id, requestModel).pipe(
                finalize(() => {
                  this.isSubmitting = false;
                }),
              );
            }),
          );
        }),
      )
      .subscribe(this.completeReturnObserver());
  }

  private completeReturnObserver(): PartialObserver<CompleteRentalReturnResponseModel> {
    return {
      next: (response: CompleteRentalReturnResponseModel) => {
        this.notificationService.success(
          `Devolução registrada com sucesso. Valor final a pagar: R$ ${response.finalAmountToPay.toFixed(2)}.`,
        );
      },
      error: (err) => {
        this.notificationService.error(err?.error ?? 'Erro ao registrar devolução do aluguel.');
      },
      complete: () => {
        this.router.navigate(['/alugueis']);
      },
    };
  }

  public goBack(): void {
    this.router.navigate(['/alugueis']);
  }
}
