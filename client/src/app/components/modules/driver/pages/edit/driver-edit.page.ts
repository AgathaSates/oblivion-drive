import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';

import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take, tap } from 'rxjs/operators';

import {
  DriverDetailModel,
  UpdateDriverRequestModel,
  UpdateDriverResponseModel,
} from '../../models/driver.models';
import { DriverService } from '../../services/driver.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { ClientDetailModel, ClientType } from '../../../client/models/client.models';
import { ClientService } from '../../../client/services/client.service';

@Component({
  selector: 'app-driver-edit',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    AsyncPipe,
    ReactiveFormsModule,
  ],
  templateUrl: './driver-edit.page.html',
})
export class DriverEditPage {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly driverService = inject(DriverService);
  private readonly clientService = inject(ClientService);
  private readonly notificationService = inject(NotificationService);

  protected readonly driverForm: FormGroup = this.fb.group({
    clientId: [null, [Validators.required]],
    isClientAlsoDriver: [false],
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    cpf: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(20)]],
    cnh: ['', [Validators.required]],
    cnhExpirationDate: ['', [Validators.required]],
  });

  protected clients: ClientDetailModel[] = [];
  protected selectedClient: ClientDetailModel | null = null;
  protected isIndividualClientSelected = false;
  protected readonly clientTypeEnum = ClientType;

  constructor() {
    this.loadClients();
    this.configureClientAndCheckboxBehavior();
  }

  get clientIdControl() {
    return this.driverForm.get('clientId');
  }

  get isClientAlsoDriverControl() {
    return this.driverForm.get('isClientAlsoDriver');
  }

  get nameControl() {
    return this.driverForm.get('name');
  }

  get emailControl() {
    return this.driverForm.get('email');
  }

  get cpfControl() {
    return this.driverForm.get('cpf');
  }

  get phoneNumberControl() {
    return this.driverForm.get('phoneNumber');
  }

  get cnhControl() {
    return this.driverForm.get('cnh');
  }

  get cnhExpirationDateControl() {
    return this.driverForm.get('cnhExpirationDate');
  }

  private loadClients(): void {
    this.clientService
      .getAllClients()
      .pipe(take(1))
      .subscribe((clients) => {
        this.clients = clients;

        const currentClientId: string | null = this.clientIdControl?.value ?? null;
        if (currentClientId) {
          this.updateSelectedClient(currentClientId);
        }
      });
  }

  private configureClientAndCheckboxBehavior(): void {
    const clientIdControl = this.clientIdControl;
    const isClientAlsoDriverControl = this.isClientAlsoDriverControl;

    if (!clientIdControl || !isClientAlsoDriverControl) {
      return;
    }

    clientIdControl.valueChanges.subscribe((clientId: string | null) => {
      this.updateSelectedClient(clientId);

      if (!this.isIndividualClientSelected) {
        isClientAlsoDriverControl.setValue(false, { emitEvent: false });
      }
    });

    isClientAlsoDriverControl.valueChanges.subscribe((isChecked: boolean) => {
      if (!isChecked) {
        return;
      }

      if (!this.selectedClient || this.selectedClient.clientType !== ClientType.Individual) {
        return;
      }

      this.driverForm.patchValue({
        name: this.selectedClient.name,
        email: this.selectedClient.email,
        cpf: this.selectedClient.cpf,
      });
    });
  }

  private updateSelectedClient(clientId: string | null): void {
    if (!clientId) {
      this.selectedClient = null;
      this.isIndividualClientSelected = false;
      return;
    }

    const client = this.clients.find((c) => c.id === clientId) ?? null;
    this.selectedClient = client;
    this.isIndividualClientSelected = !!client && client.clientType === ClientType.Individual;
  }

  protected readonly driver$ = this.route.data.pipe(
    filter((data) => !!data['driver']),
    map((data) => data['driver'] as DriverDetailModel),
    tap((driver) => {
      this.driverForm.patchValue({
        clientId: driver.clientId,
        isClientAlsoDriver: driver.isClientAlsoDriver,
        name: driver.name,
        email: driver.email,
        cpf: driver.cpf,
        phoneNumber: driver.phoneNumber,
        cnh: driver.cnh,
        cnhExpirationDate: driver.cnhExpirationDate,
      });
    }),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public save(): void {
    if (this.driverForm.invalid) {
      this.driverForm.markAllAsTouched();
      return;
    }

    const formValue = this.driverForm.value;

    const request: UpdateDriverRequestModel = {
      name: formValue.name,
      email: formValue.email,
      phoneNumber: formValue.phoneNumber,
      cpf: formValue.cpf,
      cnh: formValue.cnh,
      cnhExpirationDate: formValue.cnhExpirationDate,
      clientId: formValue.clientId,
      isClientAlsoDriver: !!formValue.isClientAlsoDriver,
    };

    const observer: PartialObserver<UpdateDriverResponseModel> = {
      next: () => {
        this.notificationService.success('Condutor atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err.error ?? 'Erro ao atualizar condutor.');
      },
      complete: () => {
        this.router.navigate(['/condutores']);
      },
    };

    this.driver$
      .pipe(
        take(1),
        switchMap((driver) => this.driverService.updateDriver(driver.id, request)),
      )
      .subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/condutores']);
  }
}
