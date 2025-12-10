import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { startWith } from 'rxjs/operators';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import {
  ClientType,
  RegisterClientRequestModel,
  RegisterClientResponseModel,
} from '../../models/client.models';
import { ClientService } from '../../services/client.service';
import { NotificationService } from '../../../../shared/notification/notification.service';

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
export class ClientCreatePage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly clientService = inject(ClientService);
  private readonly notificationService = inject(NotificationService);

  protected readonly clientTypeOptions = [
    { value: ClientType.Individual, label: 'Pessoa Física' },
    { value: ClientType.LegalEntity, label: 'Pessoa Jurídica' },
  ];

  protected readonly clientTypeEnum = ClientType;

  protected registerClientForm: FormGroup = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    phoneNumber: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(20)]],
    clientType: [null, [Validators.required]],
    cpf: [null],
    rg: [null],
    cnh: [null],
    cnpj: [null],
    state: ['', [Validators.required]],
    city: ['', [Validators.required]],
    district: ['', [Validators.required]],
    street: ['', [Validators.required]],
    number: ['', [Validators.required]],
  });

  constructor() {
    this.configureClientTypeValidation();
  }

  get nameControl() {
    return this.registerClientForm.get('name');
  }

  get emailControl() {
    return this.registerClientForm.get('email');
  }

  get phoneNumberControl() {
    return this.registerClientForm.get('phoneNumber');
  }

  get clientTypeControl() {
    return this.registerClientForm.get('clientType');
  }

  get cpfControl() {
    return this.registerClientForm.get('cpf');
  }

  get rgControl() {
    return this.registerClientForm.get('rg');
  }

  get cnhControl() {
    return this.registerClientForm.get('cnh');
  }

  get cnpjControl() {
    return this.registerClientForm.get('cnpj');
  }

  get stateControl() {
    return this.registerClientForm.get('state');
  }

  get cityControl() {
    return this.registerClientForm.get('city');
  }

  get districtControl() {
    return this.registerClientForm.get('district');
  }

  get streetControl() {
    return this.registerClientForm.get('street');
  }

  get numberControl() {
    return this.registerClientForm.get('number');
  }

  private configureClientTypeValidation(): void {
    const clientTypeControl = this.clientTypeControl;
    const cpfControl = this.cpfControl;
    const rgControl = this.rgControl;
    const cnhControl = this.cnhControl;
    const cnpjControl = this.cnpjControl;

    if (!clientTypeControl || !cpfControl || !rgControl || !cnhControl || !cnpjControl) {
      return;
    }

    clientTypeControl.valueChanges
      .pipe(startWith(clientTypeControl.value as ClientType | null))
      .subscribe((clientType: ClientType | null) => {
        if (clientType === ClientType.Individual) {
          cpfControl.setValidators([Validators.required]);
          rgControl.setValidators([Validators.required]);
          cnhControl.setValidators([Validators.required]);

          cnpjControl.clearValidators();
          cnpjControl.setValue(null);
        } else if (clientType === ClientType.LegalEntity) {
          cnpjControl.setValidators([Validators.required]);

          cpfControl.clearValidators();
          rgControl.clearValidators();
          cnhControl.clearValidators();

          cpfControl.setValue(null);
          rgControl.setValue(null);
          cnhControl.setValue(null);
        } else {
          cpfControl.clearValidators();
          rgControl.clearValidators();
          cnhControl.clearValidators();
          cnpjControl.clearValidators();
        }

        cpfControl.updateValueAndValidity({ emitEvent: false });
        rgControl.updateValueAndValidity({ emitEvent: false });
        cnhControl.updateValueAndValidity({ emitEvent: false });
        cnpjControl.updateValueAndValidity({ emitEvent: false });
      });
  }

  public registerClient(): void {
    if (this.registerClientForm.invalid) {
      this.registerClientForm.markAllAsTouched();
      return;
    }

    const formValue = this.registerClientForm.value;

    const requestModel: RegisterClientRequestModel = {
      name: formValue.name,
      email: formValue.email,
      phoneNumber: formValue.phoneNumber,
      clientType: formValue.clientType as ClientType,
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
        this.notificationService.success(
          `O cliente "${response?.name ?? requestModel.name}" foi cadastrado com sucesso!`,
        );
        this.router.navigate(['/clientes']);
      },
      error: (err) => {
        this.notificationService.error(err?.error ?? 'Erro ao cadastrar cliente.');
      },
    };

    this.clientService.registerClient(requestModel).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/clientes']);
  }
}
