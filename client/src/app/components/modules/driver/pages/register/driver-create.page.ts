import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';

import { PartialObserver } from 'rxjs';
import { take } from 'rxjs/operators';

import {
  RegisterDriverRequestModel,
  RegisterDriverResponseModel,
} from '../../models/driver.models';
import { DriverService } from '../../services/driver.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { ClientService } from '../../../client/services/client.service';
import { ClientDetailModel, ClientType } from '../../../client/models/client.models';

@Component({
  selector: 'app-driver-create',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    ReactiveFormsModule,
  ],
  templateUrl: './driver-create.page.html',
})
export class DriverCreatePage {
  private readonly fb = inject(FormBuilder);
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

  public submit(): void {
    if (this.driverForm.invalid) {
      this.driverForm.markAllAsTouched();
      return;
    }

    const formValue = this.driverForm.value;

    const request: RegisterDriverRequestModel = {
      name: formValue.name,
      email: formValue.email,
      phoneNumber: formValue.phoneNumber,
      cpf: formValue.cpf,
      cnh: formValue.cnh,
      cnhExpirationDate: formValue.cnhExpirationDate,
      clientId: formValue.clientId,
      isClientAlsoDriver: !!formValue.isClientAlsoDriver,
    };

    const observer: PartialObserver<RegisterDriverResponseModel> = {
      next: (response) => {
        this.notificationService.success(
          `O condutor "${response?.name ?? request.name}" foi cadastrado com sucesso!`,
        );
        this.router.navigate(['/condutores']);
      },
      error: (err) => {
        this.notificationService.error(err.error ?? 'Erro ao cadastrar condutor.');
      },
    };

    this.driverService.registerDriver(request).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/condutores']);
  }
}
