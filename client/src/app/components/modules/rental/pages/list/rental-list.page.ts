import { AsyncPipe, CurrencyPipe, DatePipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

import { combineLatest, map } from 'rxjs';

import { RentalDetailModel, RentalPlanTypeModel } from '../../models/rental.models';
import { RentalService } from '../../services/rental.service';
import { DriverDetailModel } from '../../../driver/models/driver.models';
import { DriverService } from '../../../driver/services/driver.service';
import { VehicleDetailModel } from '../../../vehicle/models/vehicle.models';
import { VehicleService } from '../../../vehicle/services/vehicle.service';

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
}
