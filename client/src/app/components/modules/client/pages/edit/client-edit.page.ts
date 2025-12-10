import { AsyncPipe, CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take, tap } from 'rxjs/operators';
import { startWith } from 'rxjs/operators';

import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  ClientDetailModel,
  ClientType,
  UpdateClientRequestModel,
  UpdateClientResponseModel,
} from '../../models/client.models';
import { ClientService } from '../../services/client.service';

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
    ReactiveFormsModule,
    AsyncPipe,
    CommonModule,
  ],
  templateUrl: './client-edit.page.html',
})
export class ClientEditPage implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
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

  protected readonly client$ = this.route.data.pipe(
    filter((data) => !!data['client']),
    map((data) => data['client'] as ClientDetailModel),
    tap((client) =>
      this.form.patchValue({
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
      }),
    ),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

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

  public save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.value;

    const request: UpdateClientRequestModel = {
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

    const updateObserver: PartialObserver<UpdateClientResponseModel> = {
      next: () => {
        this.notificationService.success('Cliente atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/clientes']);
      },
    };

    this.client$
      .pipe(
        take(1),
        switchMap((client) => this.clientService.updateClient(client.id, request)),
      )
      .subscribe(updateObserver);
  }

  public goBack(): void {
    this.router.navigate(['/clientes']);
  }
}
