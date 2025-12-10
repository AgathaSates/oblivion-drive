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

import { filter, map, PartialObserver, shareReplay, tap } from 'rxjs';

import {
  FuelType,
  FuelTypeLabels,
  RegisterVehicleRequestModel,
  RegisterVehicleResponseModel,
} from '../../models/vehicle.models';
import { VehicleService } from '../../services/vehicle.service';
import { VehicleGroupDetailModel } from '../../../vehicle-groups/models/vehicle-group.models';
import { NotificationService } from '../../../../shared/notification/notification.service';

@Component({
  selector: 'app-vehicle-create',
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
  ],
  templateUrl: './vehicle-create.page.html',
})
export class VehicleCreatePage {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly vehicleService = inject(VehicleService);
  private readonly notificationService = inject(NotificationService);

  private photoBytesBase64: string | null = null;
  protected photoPreviewUrl: string | null = null;

  protected readonly form: FormGroup = this.fb.group({
    licensePlate: ['', [Validators.required]],
    brand: ['', [Validators.required]],
    model: ['', [Validators.required]],
    color: ['', [Validators.required]],
    fuelType: [FuelType.Gasoline, [Validators.required]],
    fuelTankCapacityInLiters: [null, [Validators.required, Validators.min(0)]],
    year: [null, [Validators.required, Validators.min(0)]],
    vehicleGroupId: ['', [Validators.required]],
    photo: [null, [Validators.required]],
  });

  get licensePlateControl() {
    return this.form.get('licensePlate');
  }

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

  get photoControl() {
    return this.form.get('photo');
  }

  protected readonly fuelTypes = Object.values(FuelType).filter(
    (value) => typeof value === 'number',
  ) as FuelType[];

  protected readonly fuelTypeLabels = FuelTypeLabels;

  protected readonly vehicleGroups$ = this.route.data.pipe(
    map((data) => (data['vehicleGroups'] as VehicleGroupDetailModel[]) ?? []),
    tap((vehicleGroups) => {
      if (!vehicleGroups || vehicleGroups.length === 0) {
        this.notificationService.warning(
          'Não é possível cadastrar veículo sem categorias de veículos cadastradas.',
        );
        this.router.navigate(['/categorias']);
      }
    }),
    filter((vehicleGroups) => !!vehicleGroups && vehicleGroups.length > 0),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file: File | null = input.files && input.files.length > 0 ? input.files[0] : null;

    if (!file) {
      this.photoBytesBase64 = null;
      this.photoPreviewUrl = null;
      this.photoControl?.setValue(null);
      this.photoControl?.markAsTouched();
      this.photoControl?.updateValueAndValidity();
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

        this.photoControl?.setValue(file.name);
        this.photoControl?.markAsDirty();
        this.photoControl?.updateValueAndValidity();
      }
    };

    reader.readAsDataURL(file);
  }

  public submit(): void {
    if (this.form.invalid || !this.photoBytesBase64) {
      this.form.markAllAsTouched();

      if (!this.photoBytesBase64) {
        this.notificationService.warning('Selecione uma foto do veículo antes de cadastrar.');
        this.photoControl?.setErrors({ required: true });
        this.photoControl?.markAsTouched();
      }

      return;
    }

    const request: RegisterVehicleRequestModel = {
      licensePlate: this.form.value.licensePlate,
      brand: this.form.value.brand,
      model: this.form.value.model,
      color: this.form.value.color,
      fuelType: this.form.value.fuelType,
      fuelTankCapacityInLiters: this.form.value.fuelTankCapacityInLiters,
      year: this.form.value.year,
      vehicleGroupId: this.form.value.vehicleGroupId,
      photoBytes: this.photoBytesBase64,
    };

    const observer: PartialObserver<RegisterVehicleResponseModel> = {
      next: (response) => {
        this.notificationService.success(
          `O veículo com placa "${response?.licensePlate ?? request.licensePlate}" foi cadastrado com sucesso!`,
        );
        this.router.navigate(['/veiculos']);
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
    };

    this.vehicleService.registerVehicle(request).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/veiculos']);
  }
}
