import { AsyncPipe, CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take, tap } from 'rxjs/operators';

import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  FuelType,
  FuelTypeLabels,
  GetVehicleByIdResponseModel,
  UpdateVehicleRequestModel,
  UpdateVehicleResponseModel,
} from '../../models/vehicle.models';
import { VehicleService } from '../../services/vehicle.service';
import { VehicleGroupDetailModel } from '../../../vehicle-groups/models/vehicle-group.models';

@Component({
  selector: 'app-vehicle-edit',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
    AsyncPipe,
    CommonModule,
  ],
  templateUrl: './vehicle-edit.page.html',
})
export class VehicleEditPage {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly vehicleService = inject(VehicleService);
  private readonly notificationService = inject(NotificationService);

  private photoBytesBase64: string | null = null;

  protected photoPreviewUrl: string | null = null;

  protected readonly form: FormGroup = this.fb.group({
    brand: ['', [Validators.required]],
    model: ['', [Validators.required]],
    color: ['', [Validators.required]],
    fuelType: [FuelType.Gasoline, [Validators.required]],
    fuelTankCapacityInLiters: [null, [Validators.required, Validators.min(0)]],
    year: [null, [Validators.required, Validators.min(0)]],
    vehicleGroupId: ['', [Validators.required]],
  });

  get brandControl() {
    return this.form.get('brand');
  }

  get modelControl() {
    return this.form.get('model');
  }

  get colorControl() {
    return this.form.get('color');
  }

  get fuelTypeControl() {
    return this.form.get('fuelType');
  }

  get fuelTankCapacityInLitersControl() {
    return this.form.get('fuelTankCapacityInLiters');
  }

  get yearControl() {
    return this.form.get('year');
  }

  get vehicleGroupIdControl() {
    return this.form.get('vehicleGroupId');
  }

  protected readonly fuelTypes = Object.values(FuelType).filter(
    (value) => typeof value === 'number',
  ) as FuelType[];

  protected readonly fuelTypeLabels = FuelTypeLabels;

  protected readonly vehicleGroups$ = this.route.data.pipe(
    filter((data) => !!data['vehicleGroups']),
    map((data) => data['vehicleGroups'] as VehicleGroupDetailModel[]),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected readonly vehicle$ = this.route.data.pipe(
    filter((data) => !!data['vehicle']),
    map((data) => data['vehicle'] as GetVehicleByIdResponseModel),
    tap((vehicle) => {
      this.form.patchValue({
        brand: vehicle.brand,
        model: vehicle.model,
        color: vehicle.color,
        fuelType: vehicle.fuelType,
        fuelTankCapacityInLiters: vehicle.fuelTankCapacityInLiters,
        year: vehicle.year,
        vehicleGroupId: vehicle.vehicleGroupId,
      });

      if (vehicle.photoBytes) {
        this.photoBytesBase64 = vehicle.photoBytes;
        this.photoPreviewUrl = `data:image/jpeg;base64,${vehicle.photoBytes}`;
      }
    }),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file: File | null = input.files && input.files.length > 0 ? input.files[0] : null;

    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const result = reader.result;
      if (typeof result === 'string') {
        const base64Index = result.indexOf('base64,');
        const base64 = base64Index >= 0 ? result.substring(base64Index + 'base64,'.length) : result;

        this.photoBytesBase64 = base64;
        this.photoPreviewUrl = `data:${file.type};base64,${base64}`;
      }
    };

    reader.readAsDataURL(file);
  }

  public save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: UpdateVehicleRequestModel = {
      brand: this.form.value.brand,
      model: this.form.value.model,
      color: this.form.value.color,
      fuelType: this.form.value.fuelType,
      fuelTankCapacityInLiters: this.form.value.fuelTankCapacityInLiters,
      year: this.form.value.year,
      vehicleGroupId: this.form.value.vehicleGroupId,
      photoBytes: this.photoBytesBase64,
    };

    const updateObserver: PartialObserver<UpdateVehicleResponseModel> = {
      next: () => {
        this.notificationService.success('Veículo atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/veiculos']);
      },
    };

    this.vehicle$
      .pipe(
        take(1),
        switchMap((vehicle) => this.vehicleService.updateVehicle(vehicle.id, request)),
      )
      .subscribe(updateObserver);
  }

  public goBack(): void {
    this.router.navigate(['/veiculos']);
  }
}
