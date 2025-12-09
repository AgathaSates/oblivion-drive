import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take, tap } from 'rxjs/operators';

import { EmployeeService } from '../../services/employee.service';
import {
  EmployeeDetailModel,
  UpdateEmployeeByCompanyRequestModel,
  UpdateEmployeeByCompanyResponseModel,
} from '../../models/employee.models';
import { NotificationService } from '../../../../shared/notification/notification.service';

@Component({
  selector: 'app-employee-edit',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    AsyncPipe,
    ReactiveFormsModule,
  ],
  templateUrl: './employee-edit.page.html',
})
export class EmployeeEditPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly employeeService = inject(EmployeeService);
  private readonly notificationService = inject(NotificationService);

  protected employeeForm: FormGroup = this.formBuilder.group({
    name: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(200),
        Validators.pattern(/^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$/),
      ],
    ],
    hireDate: ['', [Validators.required]],
    salary: ['', [Validators.required, Validators.min(0.01), Validators.max(1_000_000)]],
  });

  get nameControl() {
    return this.employeeForm.get('name');
  }

  get hireDateControl() {
    return this.employeeForm.get('hireDate');
  }

  get salaryControl() {
    return this.employeeForm.get('salary');
  }

  protected readonly employee$ = this.route.data.pipe(
    filter((data) => !!data['employee']),
    map((data) => data['employee'] as EmployeeDetailModel),
    tap((employee) =>
      this.employeeForm.patchValue({
        name: employee.name,
        hireDate: employee.hireDate,
        salary: employee.salary,
      }),
    ),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public save(): void {
    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      return;
    }

    const requestModel: UpdateEmployeeByCompanyRequestModel = {
      name: this.employeeForm.value.name,
      hireDate: this.employeeForm.value.hireDate,
      salary: this.employeeForm.value.salary,
    };

    const updateObserver: PartialObserver<UpdateEmployeeByCompanyResponseModel> = {
      next: () => {
        this.notificationService.success('Funcionário atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/funcionarios']);
      },
    };

    this.employee$
      .pipe(
        take(1),
        switchMap((employee) =>
          this.employeeService.updateEmployeeByCompany(employee.id, requestModel),
        ),
      )
      .subscribe(updateObserver);
  }

  public goBack(): void {
    this.router.navigate(['/funcionarios']);
  }
}
