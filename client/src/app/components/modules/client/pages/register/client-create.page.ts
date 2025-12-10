import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { PartialObserver } from 'rxjs';
import { startWith } from 'rxjs/operators';

import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  ClientType,
  RegisterClientRequestModel,
  RegisterClientResponseModel,
} from '../../models/client.models';
import { ClientService } from '../../services/client.service';

@Component({
  selector: 'app-client-create',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './client-create.page.html',
})
export class ClientCreatePage implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly clientService = inject(ClientService);
  private readonly notificationService = inject(NotificationService);

  protected readonly ClientType = ClientType;

  protected readonly form: FormGroup = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(20)]],
    clientType: [ClientType.Individual, [Validators.required]],
    cpf: [''],
    rg: [''],
    cnh: [''],
    cnpj: [''],
    state: ['', [Validators.required, Validators.maxLength(100)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    district: ['', [Validators.required, Validators.maxLength(100)]],
    street: ['', [Validators.required, Validators.maxLength(200)]],
    number: ['', [Validators.required, Validators.maxLength(20)]],
  });

  get nameControl() {
    return this.form.get('name');
  }

  get emailControl() {
    return this.form.get('email');
  }

  get phoneNumberControl() {
    return this.form.get('phoneNumber');
  }

  get clientTypeControl() {
    return this.form.get('clientType');
  }

  get cpfControl() {
    return this.form.get('cpf');
  }

  get rgControl() {
    return this.form.get('rg');
  }

  get cnhControl() {
    return this.form.get('cnh');
  }

  get cnpjControl() {
    return this.form.get('cnpj');
  }

  get stateControl() {
    return this.form.get('state');
  }

  get cityControl() {
    return this.form.get('city');
  }

  get districtControl() {
    return this.form.get('district');
  }

  get streetControl() {
    return this.form.get('street');
  }

  get numberControl() {
    return this.form.get('number');
  }

  public ngOnInit(): void {
    this.configureClientTypeValidators();
  }

  private configureClientTypeValidators(): void {
    const cpfControl = this.cpfControl;
    const rgControl = this.rgControl;
    const cnhControl = this.cnhControl;
    const cnpjControl = this.cnpjControl;
    const clientTypeControl = this.clientTypeControl;

    if (!cpfControl || !rgControl || !cnhControl || !cnpjControl || !clientTypeControl) {
      return;
    }

    clientTypeControl.valueChanges.pipe(startWith(clientTypeControl.value)).subscribe((value) => {
      const selectedType = value as ClientType;

      if (selectedType === ClientType.Individual) {
        cpfControl.setValidators([Validators.required]);
        rgControl.setValidators([Validators.required]);
        cnhControl.setValidators([Validators.required]);
        cnpjControl.clearValidators();
      } else {
        cpfControl.clearValidators();
        rgControl.clearValidators();
        cnhControl.clearValidators();
        cnpjControl.setValidators([Validators.required]);
      }

      cpfControl.updateValueAndValidity({ emitEvent: false });
      rgControl.updateValueAndValidity({ emitEvent: false });
      cnhControl.updateValueAndValidity({ emitEvent: false });
      cnpjControl.updateValueAndValidity({ emitEvent: false });
    });
  }

  public submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.value;

    const request: RegisterClientRequestModel = {
      name: formValue.name,
      email: formValue.email,
      phoneNumber: formValue.phoneNumber,
      clientType: formValue.clientType,
      cpf: formValue.cpf ?? null,
      rg: formValue.rg ?? null,
      cnh: formValue.cnh ?? null,
      cnpj: formValue.cnpj ?? null,
      state: formValue.state,
      city: formValue.city,
      district: formValue.district,
      street: formValue.street,
      number: formValue.number,
    };

    const observer: PartialObserver<RegisterClientResponseModel> = {
      next: (response) => {
        const clientName: string = response?.name ?? request.name;
        this.notificationService.success(`O cliente "${clientName}" foi cadastrado com sucesso!`);
        this.router.navigate(['/clientes']);
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
    };

    this.clientService.registerClient(request).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/clientes']);
  }
}
