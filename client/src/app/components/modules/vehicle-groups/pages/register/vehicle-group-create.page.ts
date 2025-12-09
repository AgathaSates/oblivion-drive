import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { VehicleGroupService } from '../../services/vehicle-group.service';
import { NotificationService } from '../../../../shared/notification/notification.service';

import {
  RegisterVehicleGroupRequestModel,
  RegisterVehicleGroupResponseModel,
} from '../../models/vehicle-group.models';
import { PartialObserver } from 'rxjs';

@Component({
  selector: 'app-vehicle-group-create',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './vehicle-group-create.page.html',
})
export class VehicleGroupCreatePage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly vehicleGroupService = inject(VehicleGroupService);
  private readonly notificationService = inject(NotificationService);

  protected vehicleGroupForm: FormGroup = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
  });

  get nameControl() {
    return this.vehicleGroupForm.get('name');
  }

  public registerVehicleGroup(): void {
    if (this.vehicleGroupForm.invalid) {
      this.vehicleGroupForm.markAllAsTouched();
      return;
    }

    const requestModel: RegisterVehicleGroupRequestModel = {
      name: this.vehicleGroupForm.value.name,
    };

    const observer: PartialObserver<RegisterVehicleGroupResponseModel> = {
      next: (response) => {
        this.notificationService.success(
          `O grupo "${response?.name ?? requestModel.name}" foi cadastrado com sucesso!`,
        );
        this.router.navigate(['/categorias']);
      },
      error: (err) => this.notificationService.error(err.error),
    };

    this.vehicleGroupService.registerVehicleGroup(requestModel).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/categorias']);
  }
}
