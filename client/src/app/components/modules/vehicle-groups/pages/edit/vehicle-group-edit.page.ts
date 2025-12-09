import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take, tap } from 'rxjs/operators';

import { VehicleGroupService } from '../../services/vehicle-group.service';
import { NotificationService } from '../../../../shared/notification/notification.service';

import {
  VehicleGroupDetailModel,
  UpdateVehicleGroupRequestModel,
  UpdateVehicleGroupResponseModel,
} from '../../models/vehicle-group.models';

@Component({
  selector: 'app-vehicle-group-edit',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
    AsyncPipe,
  ],
  templateUrl: './vehicle-group-edit.page.html',
})
export class VehicleGroupEditPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly vehicleGroupService = inject(VehicleGroupService);
  private readonly notificationService = inject(NotificationService);

  protected vehicleGroupForm: FormGroup = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
  });

  get nameControl() {
    return this.vehicleGroupForm.get('name');
  }

  protected readonly vehicleGroup$ = this.route.data.pipe(
    filter((data) => !!data['vehicleGroup']),
    map((data) => data['vehicleGroup'] as VehicleGroupDetailModel),
    tap((vehicleGroup) =>
      this.vehicleGroupForm.patchValue({
        name: vehicleGroup.name,
      }),
    ),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public save(): void {
    if (this.vehicleGroupForm.invalid) {
      this.vehicleGroupForm.markAllAsTouched();
      return;
    }

    const requestModel: UpdateVehicleGroupRequestModel = {
      name: this.vehicleGroupForm.value.name,
    };

    const observer: PartialObserver<UpdateVehicleGroupResponseModel> = {
      next: () => {
        this.notificationService.success('Grupo de veículos atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/categorias']);
      },
    };

    this.vehicleGroup$
      .pipe(
        take(1),
        switchMap((vehicleGroup) =>
          this.vehicleGroupService.updateVehicleGroup(vehicleGroup.id, requestModel),
        ),
      )
      .subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/categorias']);
  }
}
