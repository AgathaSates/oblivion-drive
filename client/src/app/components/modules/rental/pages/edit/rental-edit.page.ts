import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { filter, map, shareReplay, switchMap, take, tap } from 'rxjs/operators';
import { PartialObserver } from 'rxjs';

import {
  RentalDetailModel,
  RentalPlanTypeModel,
  UpdateRentalRequestModel,
  UpdateRentalResponseModel,
} from '../../models/rental.models';
import { RentalService } from '../../services/rental.service';
import { NotificationService } from '../../../../shared/notification/notification.service';

import { ClientDetailModel } from '../../../client/models/client.models';
import { DriverDetailModel } from '../../../driver/models/driver.models';
import { VehicleDetailModel } from '../../../vehicle/models/vehicle.models';
import { VehicleGroupDetailModel } from '../../../vehicle-groups/models/vehicle-group.models';
import { ServiceDetailModel } from '../../../services/models/service.models';

@Component({
  selector: 'app-rental-edit',
  standalone: true,
  imports: [
    AsyncPipe,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './rental-edit.page.html',
})
export class RentalEditPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly rentalService = inject(RentalService);
  private readonly notificationService = inject(NotificationService);

  protected readonly rentalForm: FormGroup = this.formBuilder.group({
    clientId: [null, [Validators.required]],
    driverId: [null, [Validators.required]],
    vehicleGroupId: [null, [Validators.required]],
    vehicleId: [null, [Validators.required]],
    planType: [null, [Validators.required]],
    startDate: [null, [Validators.required]],
    expectedReturnDate: [null, [Validators.required]],
    insuranceDailyPricePerPerson: [0, [Validators.required, Validators.min(0)]],
    insurancePersonsCount: [1, [Validators.required, Validators.min(1)]],
    estimatedTotalKilometers: [null, [Validators.min(0)]],
    serviceIds: [[] as string[]],
  });

  protected readonly planTypeOptions = [
    { value: RentalPlanTypeModel.Daily, label: 'Plano Diário' },
    { value: RentalPlanTypeModel.Controlled, label: 'Plano Controlado' },
    { value: RentalPlanTypeModel.Free, label: 'Plano Livre' },
  ];

  protected clients: ClientDetailModel[] = [];
  protected allDrivers: DriverDetailModel[] = [];
  protected allVehicles: VehicleDetailModel[] = [];
  protected services: ServiceDetailModel[] = [];
  protected vehicleGroups: VehicleGroupDetailModel[] = [];

  protected readonly rental$ = this.route.data.pipe(
    filter((data) => !!data['rental']),
    map((data) => data['rental'] as RentalDetailModel),
    tap((rental) => this.patchFormWithRental(rental)),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  constructor() {
    this.loadInitialDataFromResolvers();
    this.configureClientChangeBehavior();
    this.configureVehicleGroupChangeBehavior();
  }

  get clientIdControl() {
    return this.rentalForm.get('clientId');
  }

  get driverIdControl() {
    return this.rentalForm.get('driverId');
  }

  get vehicleGroupIdControl() {
    return this.rentalForm.get('vehicleGroupId');
  }

  get vehicleIdControl() {
    return this.rentalForm.get('vehicleId');
  }

  get planTypeControl() {
    return this.rentalForm.get('planType');
  }

  get startDateControl() {
    return this.rentalForm.get('startDate');
  }

  get expectedReturnDateControl() {
    return this.rentalForm.get('expectedReturnDate');
  }

  get insuranceDailyPricePerPersonControl() {
    return this.rentalForm.get('insuranceDailyPricePerPerson');
  }

  get insurancePersonsCountControl() {
    return this.rentalForm.get('insurancePersonsCount');
  }

  get estimatedTotalKilometersControl() {
    return this.rentalForm.get('estimatedTotalKilometers');
  }

  get serviceIdsControl() {
    return this.rentalForm.get('serviceIds');
  }

  protected get driversForSelectedClient(): DriverDetailModel[] {
    const selectedClientId: string | null = this.clientIdControl?.value ?? null;
    if (!selectedClientId) {
      return [];
    }

    return this.allDrivers.filter((driver) => driver.clientId === selectedClientId);
  }

  protected get vehiclesForSelectedGroup(): VehicleDetailModel[] {
    const selectedGroupId: string | null = this.vehicleGroupIdControl?.value ?? null;
    if (!selectedGroupId) {
      return [];
    }

    return this.allVehicles.filter((vehicle) => vehicle.vehicleGroupId === selectedGroupId);
  }

  private loadInitialDataFromResolvers(): void {
    const routeData = this.route.snapshot.data;

    this.clients = (routeData['clients'] as ClientDetailModel[]) ?? [];
    this.allDrivers = (routeData['drivers'] as DriverDetailModel[]) ?? [];
    this.allVehicles = (routeData['vehicles'] as VehicleDetailModel[]) ?? [];
    this.services = (routeData['services'] as ServiceDetailModel[]) ?? [];
    this.vehicleGroups = (routeData['vehicleGroups'] as VehicleGroupDetailModel[]) ?? [];
  }

  private patchFormWithRental(rental: RentalDetailModel): void {
    const vehicle = this.allVehicles.find((v) => v.id === rental.vehicleId);
    const vehicleGroupId: string | null = vehicle?.vehicleGroupId ?? null;

    const rentalAsAny: any = rental;

    this.rentalForm.patchValue({
      clientId: rental.clientId,
      driverId: rental.driverId,
      vehicleGroupId,
      vehicleId: rental.vehicleId,
      planType: rental.planType,
      startDate: rental.startDate,
      expectedReturnDate: rental.expectedReturnDate,
      insuranceDailyPricePerPerson:
        rentalAsAny.insuranceDailyPricePerPerson ?? this.insuranceDailyPricePerPersonControl?.value,
      insurancePersonsCount:
        rentalAsAny.insurancePersonsCount ?? this.insurancePersonsCountControl?.value,
      estimatedTotalKilometers:
        rentalAsAny.estimatedTotalKilometers ?? this.estimatedTotalKilometersControl?.value,
      serviceIds: rental.serviceIds ?? [],
    });
  }

  private configureClientChangeBehavior(): void {
    this.clientIdControl?.valueChanges.subscribe((clientId: string | null) => {
      this.driverIdControl?.setValue(null);

      if (!clientId) {
        return;
      }

      const hasDriversForClient: boolean = this.allDrivers.some(
        (driver) => driver.clientId === clientId,
      );

      if (!hasDriversForClient) {
        this.notificationService.warning(
          'O cliente selecionado não possui condutores cadastrados. ' +
            'Cadastre um condutor antes de criar o aluguel.',
        );
      }
    });
  }

  private configureVehicleGroupChangeBehavior(): void {
    this.vehicleGroupIdControl?.valueChanges.subscribe((vehicleGroupId: string | null) => {
      this.vehicleIdControl?.setValue(null);

      if (!vehicleGroupId) {
        return;
      }

      const hasVehiclesForGroup: boolean = this.allVehicles.some(
        (vehicle) => vehicle.vehicleGroupId === vehicleGroupId,
      );

      if (!hasVehiclesForGroup) {
        this.notificationService.warning(
          'A categoria selecionada não possui veículos cadastrados. ' +
            'Cadastre um veículo nesta categoria antes de criar o aluguel.',
        );
      }
    });
  }

  public save(): void {
    if (this.rentalForm.invalid) {
      this.rentalForm.markAllAsTouched();
      return;
    }

    const formValue = this.rentalForm.value;

    const estimatedTotalKilometers: number | null =
      formValue.estimatedTotalKilometers === null ||
      formValue.estimatedTotalKilometers === undefined ||
      formValue.estimatedTotalKilometers === ''
        ? null
        : Number(formValue.estimatedTotalKilometers);

    const requestModel: UpdateRentalRequestModel = {
      clientId: formValue.clientId,
      driverId: formValue.driverId,
      vehicleId: formValue.vehicleId,
      planType: formValue.planType as RentalPlanTypeModel,
      startDate: formValue.startDate,
      expectedReturnDate: formValue.expectedReturnDate,
      insuranceDailyPricePerPerson: Number(formValue.insuranceDailyPricePerPerson ?? 0),
      insurancePersonsCount: Number(formValue.insurancePersonsCount ?? 1),
      estimatedTotalKilometers,
      serviceIds: (formValue.serviceIds as string[]) ?? [],
    };

    const updateObserver: PartialObserver<UpdateRentalResponseModel> = {
      next: () => {
        this.notificationService.success('Aluguel atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err?.error ?? 'Erro ao atualizar aluguel.');
      },
      complete: () => {
        this.router.navigate(['/alugueis']);
      },
    };

    this.rental$
      .pipe(
        take(1),
        switchMap((rental) => this.rentalService.updateRental(rental.id, requestModel)),
      )
      .subscribe(updateObserver);
  }

  public goBack(): void {
    this.router.navigate(['/alugueis']);
  }
}
