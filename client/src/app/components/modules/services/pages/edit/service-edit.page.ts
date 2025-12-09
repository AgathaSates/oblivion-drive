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
import { filter, map, shareReplay, switchMap, take, tap } from 'rxjs/operators';

import {
  ServiceDetailModel,
  UpdateServiceRequestModel,
  UpdateServiceResponseModel,
  ChargeTypeModel,
} from '../../models/service.models';

import { NotificationService } from '../../../../shared/notification/notification.service';
import { ServicesService } from '../../Services.service';

@Component({
  selector: 'app-service-edit',
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
  templateUrl: './service-edit.page.html',
})
export class ServiceEditPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly serviceService = inject(ServicesService);
  private readonly notificationService = inject(NotificationService);

  protected serviceForm: FormGroup = this.formBuilder.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    price: ['', [Validators.required, Validators.min(0.01)]],
    chargeType: ['', [Validators.required]],
  });

  get nameControl() {
    return this.serviceForm.get('name');
  }

  get priceControl() {
    return this.serviceForm.get('price');
  }

  get chargeTypeControl() {
    return this.serviceForm.get('chargeType');
  }

  protected readonly chargeTypeOptions = [
    { value: ChargeTypeModel.Fixed, label: 'Valor fixo' },
    { value: ChargeTypeModel.PerDay, label: 'Cobrança por dia' },
  ];

  protected readonly service$ = this.route.data.pipe(
    filter((data) => !!data['service']),
    map((data) => data['service'] as ServiceDetailModel),
    tap((service) => {
      this.serviceForm.patchValue({
        name: service.name,
        price: service.price,
        chargeType: service.chargeType,
      });
    }),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public save(): void {
    if (this.serviceForm.invalid) {
      this.serviceForm.markAllAsTouched();
      return;
    }

    const requestModel: UpdateServiceRequestModel = {
      name: this.serviceForm.value.name,
      price: this.serviceForm.value.price,
      chargeType: this.serviceForm.value.chargeType,
    };

    const updateObserver: PartialObserver<UpdateServiceResponseModel> = {
      next: () => {
        this.notificationService.success('Serviço atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/servicos']);
      },
    };

    this.service$
      .pipe(
        take(1),
        switchMap((service) => this.serviceService.updateService(service.id, requestModel)),
      )
      .subscribe(updateObserver);
  }

  public goBack(): void {
    this.router.navigate(['/servicos']);
  }
}
