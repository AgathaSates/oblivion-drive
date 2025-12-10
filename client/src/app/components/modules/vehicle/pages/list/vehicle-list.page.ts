import { AsyncPipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

import { combineLatest, map, of, startWith, switchMap } from 'rxjs';

import { VehicleDetailModel, FuelType, FuelTypeLabels } from '../../models/vehicle.models';
import { VehicleService } from '../../services/vehicle.service';
import { VehicleGroupDetailModel } from '../../../vehicle-groups/models/vehicle-group.models';

export interface VehicleListItemViewModel extends VehicleDetailModel {
  vehicleGroupName: string;
  fuelTypeLabel: string;
  photoPreviewUrl: string;
}

@Component({
  selector: 'app-vehicle-list',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatFormFieldModule,
    MatSelectModule,
    SlicePipe,
  ],
  templateUrl: './vehicle-list.page.html',
})
export class VehicleListPage {
  private readonly route = inject(ActivatedRoute);
  private readonly vehicleService = inject(VehicleService);

  protected readonly vehicleGroupFilterControl = new FormControl<string | null>(null);

  private readonly initialVehicles$ = this.route.data.pipe(
    map((data) => (data['vehicles'] as VehicleDetailModel[]) ?? []),
  );

  protected readonly vehicleGroups$ = this.route.data.pipe(
    map((data) => (data['vehicleGroups'] as VehicleGroupDetailModel[]) ?? []),
  );

  protected readonly vehicles$ = combineLatest([
    this.initialVehicles$,
    this.vehicleGroups$,
    this.vehicleGroupFilterControl.valueChanges.pipe(startWith<string | null>(null)),
  ]).pipe(
    switchMap(([initialVehicles, vehicleGroups, selectedGroupId]) => {
      if (selectedGroupId) {
        return this.vehicleService
          .getAllVehicles(selectedGroupId)
          .pipe(map((vehicles) => this.mapToViewModel(vehicles, vehicleGroups)));
      }

      return of(this.mapToViewModel(initialVehicles, vehicleGroups));
    }),
  );

  private mapToViewModel(
    vehicles: VehicleDetailModel[],
    vehicleGroups: VehicleGroupDetailModel[],
  ): VehicleListItemViewModel[] {
    return vehicles.map<VehicleListItemViewModel>((vehicle) => {
      const group = vehicleGroups.find((g) => g.id === vehicle.vehicleGroupId);

      const fuelTypeEnum: FuelType = this.toFuelTypeEnum(vehicle.fuelType);

      const photoPreviewUrl: string =
        vehicle.photoBytes && vehicle.photoBytes.trim().length > 0
          ? `data:image/jpeg;base64,${vehicle.photoBytes}`
          : 'assets/images/vehicle-placeholder-vehicle.svg';

      return {
        ...vehicle,
        vehicleGroupName: group?.name ?? vehicle.vehicleGroupId,
        fuelTypeLabel: FuelTypeLabels[fuelTypeEnum],
        photoPreviewUrl,
      };
    });
  }

  private toFuelTypeEnum(value: VehicleDetailModel['fuelType']): FuelType {
    if (typeof value === 'number') {
      return value as FuelType;
    }

    if (typeof value === 'string') {
      const key = value as keyof typeof FuelType;
      const maybeEnumValue = FuelType[key];

      if (typeof maybeEnumValue === 'number') {
        return maybeEnumValue as FuelType;
      }
    }

    return FuelType.Gasoline;
  }
}
