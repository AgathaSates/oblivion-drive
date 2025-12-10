import { AsyncPipe, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';

import { combineLatest, PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';
import { RentalService } from '../services/rental.service';
import { NotificationService } from '../../../shared/notification/notification.service';
import {
  RentalDetailModel,
  RentalPlanTypeModel,
  CompleteRentalReturnRequestModel,
  CompleteRentalReturnResponseModel,
} from '../models/rental.models';
import { DriverService } from '../../driver/services/driver.service';
import { DriverDetailModel } from '../../driver/models/driver.models';
import { VehicleDetailModel } from '../../vehicle/models/vehicle.models';
import { VehicleService } from '../../vehicle/services/vehicle.service';

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
    RouterLink,
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

  // rental “cru” vindo do resolver
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

  // ---------- getters do formulário ----------

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
    const driver = drivers.find((d) => d.id === rental.driverId);
    const vehicle = vehicles.find((v) => v.id === rental.vehicleId);

    const driverName: string = driver?.name ?? rental.driverId;
    const vehicleLabel: string = vehicle
      ? `${vehicle.brand} ${vehicle.model} (${vehicle.licensePlate})`
      : rental.vehicleId;

    const planTypeLabel: string = this.planTypeLabels[rental.planType] ?? rental.planType;

    return {
      ...rental,
      driverName,
      vehicleLabel,
      planTypeLabel,
    };
  }

  private getTodayAsDateInput(): string {
    const today = new Date();
    const year = today.getFullYear();
    const month = `${today.getMonth() + 1}`.padStart(2, '0');
    const day = `${today.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  public completeReturn(): void {
    if (this.returnForm.invalid) {
      this.returnForm.markAllAsTouched();
      return;
    }

    const formValue = this.returnForm.value;

    const requestModel: CompleteRentalReturnRequestModel = {
      actualReturnDate: formValue.actualReturnDate,
      initialOdometerInKm: Number(formValue.initialOdometerInKm),
      currentOdometerInKm: Number(formValue.currentOdometerInKm),
      isFuelTankFullOnReturn: Boolean(formValue.isFuelTankFullOnReturn),
      hasDamage: Boolean(formValue.hasDamage),
      couponName:
        formValue.couponName && String(formValue.couponName).trim().length > 0
          ? String(formValue.couponName).trim()
          : null,
    };

    const observer: PartialObserver<CompleteRentalReturnResponseModel> = {
      next: (response) => {
        this.notificationService.success(
          `Devolução registrada com sucesso. Valor final a pagar: R$ ${response.finalAmountToPay.toFixed(
            2,
          )}.`,
        );
      },
      error: (err) => {
        this.notificationService.error(err?.error ?? 'Erro ao registrar devolução do aluguel.');
      },
      complete: () => {
        this.router.navigate(['/alugueis']);
      },
    };

    this.rental$
      .pipe(
        take(1),
        switchMap((rental) => this.rentalService.completeRentalReturn(rental.id, requestModel)),
      )
      .subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/alugueis']);
  }
}
