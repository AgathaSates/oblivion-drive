import { Component, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { EmployeeService } from '../../services/employee.service';
import {
  RegisterEmployeeRequestModel,
  RegisterEmployeeResponseModel,
} from '../../models/employee.models';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { PartialObserver } from 'rxjs';

const PASSWORD_REGEX = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$/;
const USERNAME_REGEX = /^\S+$/;
const NAME_REGEX = /^[A-Za-zÀ-ÖØ-öø-ÿ\s]+$/;

@Component({
  selector: 'app-register-employee',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './register-employee.page.html',
})
export class RegisterEmployeePage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly employeeService = inject(EmployeeService);
  private readonly notificationService = inject(NotificationService);

  private readonly minHireDate = '1970-01-01';
  protected readonly minHireDateInput = this.minHireDate;
  protected readonly maxHireDateInput = new Date().toISOString().slice(0, 10);

  private readonly hireDateRangeValidator: ValidatorFn = (
    control: AbstractControl,
  ): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return { invalidDate: true };

    const min = new Date(this.minHireDate);
    const max = new Date(this.maxHireDateInput);

    if (date < min) return { minDate: true };
    if (date > max) return { maxDate: true };

    return null;
  };

  protected registerEmployeeForm: FormGroup = this.formBuilder.group({
    userName: [
      '',
      [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(100),
        Validators.pattern(USERNAME_REGEX),
      ],
    ],
    email: ['', [Validators.required, Validators.maxLength(256), Validators.email]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(6),
        Validators.maxLength(100),
        Validators.pattern(PASSWORD_REGEX),
      ],
    ],
    name: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(200),
        Validators.pattern(NAME_REGEX),
      ],
    ],
    hireDate: ['', [Validators.required, this.hireDateRangeValidator]],
    salary: [null, [Validators.required, Validators.min(0.01), Validators.max(1_000_000)]],
  });

  get userNameControl() {
    return this.registerEmployeeForm.get('userName');
  }

  get emailControl() {
    return this.registerEmployeeForm.get('email');
  }

  get passwordControl() {
    return this.registerEmployeeForm.get('password');
  }

  get nameControl() {
    return this.registerEmployeeForm.get('name');
  }

  get hireDateControl() {
    return this.registerEmployeeForm.get('hireDate');
  }

  get salaryControl() {
    return this.registerEmployeeForm.get('salary');
  }

  public registerEmployee(): void {
    if (this.registerEmployeeForm.invalid) {
      this.registerEmployeeForm.markAllAsTouched();
      return;
    }

    const formValue = this.registerEmployeeForm.value;

    const requestModel: RegisterEmployeeRequestModel = {
      userName: formValue.userName,
      email: formValue.email,
      password: formValue.password,
      name: formValue.name,
      hireDate: formValue.hireDate,
      salary: Number(formValue.salary),
    };

    const observer: PartialObserver<RegisterEmployeeResponseModel> = {
      next: (response) => {
        this.notificationService.success(
          `O funcionário "${response?.name ?? requestModel.name}" foi cadastrado com sucesso!`,
        );
        this.router.navigate(['/funcionarios']);
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
    };

    this.employeeService.registerEmployee(requestModel).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/funcionarios']);
  }
}
