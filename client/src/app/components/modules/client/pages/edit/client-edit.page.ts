import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, startWith, switchMap, take, tap } from 'rxjs/operators';

import {
  ClientDetailModel,
  ClientType,
  UpdateClientRequestModel,
  UpdateClientResponseModel,
} from '../../models/client.models';
import { ClientService } from '../../services/client.service';
import { NotificationService } from '../../../../shared/notification/notification.service';

@Component({
  selector: 'app-client-edit',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    AsyncPipe,
    ReactiveFormsModule,
  ],
  templateUrl: './client-edit.page.html',
})
export class ClientEditPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly clientService = inject(ClientService);
  private readonly notificationService = inject(NotificationService);

  protected readonly clientTypeEnum = ClientType;

  protected readonly clientTypeOptions = [
    { value: ClientType.Individual, label: 'Pessoa Física' },
    { value: ClientType.LegalEntity, label: 'Pessoa Jurídica' },
  ];

  protected clientForm: FormGroup = this.formBuilder.group({
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
    return this.clientForm.get('name');
  }

  get emailControl() {
    return this.clientForm.get('email');
  }

  get phoneNumberControl() {
    return this.clientForm.get('phoneNumber');
  }

  get clientTypeControl() {
    return this.clientForm.get('clientType');
  }

  get cpfControl() {
    return this.clientForm.get('cpf');
  }

  get rgControl() {
    return this.clientForm.get('rg');
  }

  get cnhControl() {
    return this.clientForm.get('cnh');
  }

  get cnpjControl() {
    return this.clientForm.get('cnpj');
  }

  get stateControl() {
    return this.clientForm.get('state');
  }

  get cityControl() {
    return this.clientForm.get('city');
  }

  get districtControl() {
    return this.clientForm.get('district');
  }

  get streetControl() {
    return this.clientForm.get('street');
  }

  get numberControl() {
    return this.clientForm.get('number');
  }

  protected readonly client$ = this.route.paramMap.pipe(
    map((params) => params.get('id')),
    filter((id): id is string => !!id),
    switchMap((id) => this.clientService.getClientById(id)),
    tap((client: ClientDetailModel) => {
      this.clientForm.patchValue({
        name: client.name,
        email: client.email,
        phoneNumber: client.phoneNumber,
        clientType: client.clientType,
        cpf: client.cpf,
        rg: client.rg,
        cnh: client.cnh,
        cnpj: client.cnpj,
        state: client.state,
        city: client.city,
        district: client.district,
        street: client.street,
        number: client.number,
      });
    }),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

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

  public save(): void {
    if (this.clientForm.invalid) {
      this.clientForm.markAllAsTouched();
      return;
    }

    const formValue = this.clientForm.value;

    const requestModel: UpdateClientRequestModel = {
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

    const observer: PartialObserver<UpdateClientResponseModel> = {
      next: () => {
        this.notificationService.success('Cliente atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err?.error ?? 'Erro ao atualizar cliente.');
      },
      complete: () => {
        this.router.navigate(['/clientes']);
      },
    };

    this.client$
      .pipe(
        take(1),
        switchMap((client) => this.clientService.updateClient(client.id, requestModel)),
      )
      .subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/clientes']);
  }
}
