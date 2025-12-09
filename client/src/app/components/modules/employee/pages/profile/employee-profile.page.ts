import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { EmployeeService } from '../../services/employee.service';
import {
  UpdateOwnEmployeeRequestModel,
  UpdateOwnEmployeeResponseModel,
} from '../../models/employee.models';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { PartialObserver } from 'rxjs';

@Component({
  selector: 'app-employee-profile',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './employee-profile.page.html',
})
export class EmployeeProfilePage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly employeeService = inject(EmployeeService);
  private readonly notificationService = inject(NotificationService);

  protected profileForm: FormGroup = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(200)]],
  });

  get nameControl() {
    return this.profileForm.get('name');
  }

  public saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    const requestModel: UpdateOwnEmployeeRequestModel = {
      name: this.profileForm.value.name,
    };

    const updateObserver: PartialObserver<UpdateOwnEmployeeResponseModel> = {
      next: () => {
        this.notificationService.success('Perfil atualizado com sucesso!');
        this.router.navigate(['/inicio']);
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
    };

    this.employeeService.updateOwnEmployeeProfile(requestModel).subscribe(updateObserver);
  }

  public goBack(): void {
    this.router.navigate(['/inicio']);
  }
}
