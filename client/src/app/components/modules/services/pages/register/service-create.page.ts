import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import {
  ChargeTypeModel,
  RegisterServiceRequestModel,
  RegisterServiceResponseModel,
} from '../../models/service.models';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { PartialObserver } from 'rxjs';
import { ServicesService } from '../../Services.service';

@Component({
  selector: 'app-service-create',
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
  templateUrl: './service-create.page.html',
})
export class ServiceCreatePage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly servicesService = inject(ServicesService);
  private readonly notificationService = inject(NotificationService);

  protected readonly chargeTypeOptions = [
    { value: ChargeTypeModel.Fixed, label: 'Valor fixo' },
    { value: ChargeTypeModel.PerDay, label: 'Cobrança por dia' },
  ];

  protected registerServiceForm: FormGroup = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    price: [null, [Validators.required, Validators.min(0.01), Validators.max(1_000_000)]],
    chargeType: [null, [Validators.required]],
  });

  get nameControl() {
    return this.registerServiceForm.get('name');
  }

  get priceControl() {
    return this.registerServiceForm.get('price');
  }

  get chargeTypeControl() {
    return this.registerServiceForm.get('chargeType');
  }

  public registerService(): void {
    if (this.registerServiceForm.invalid) {
      this.registerServiceForm.markAllAsTouched();
      return;
    }

    const formValue = this.registerServiceForm.value;

    const requestModel: RegisterServiceRequestModel = {
      name: formValue.name,
      price: Number(formValue.price),
      chargeType: formValue.chargeType as ChargeTypeModel,
    };

    const observer: PartialObserver<RegisterServiceResponseModel> = {
      next: (response) => {
        this.notificationService.success(
          `O serviço "${response?.name ?? requestModel.name}" foi cadastrado com sucesso!`,
        );
        this.router.navigate(['/servicos']);
      },
      error: (err) => {
        this.notificationService.error(err.error ?? 'Erro ao cadastrar serviço.');
      },
    };

    this.servicesService.registerService(requestModel).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/servicos']);
  }
}
