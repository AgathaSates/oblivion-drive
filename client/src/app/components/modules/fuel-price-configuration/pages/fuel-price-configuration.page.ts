import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, tap } from 'rxjs/operators';
import { FuelPriceConfigurationService } from '../services/fuel-price-configuration.service';
import { NotificationService } from '../../../shared/notification/notification.service';
import {
  GetFuelPriceConfigurationResponseModel,
  UpdateFuelPriceConfigurationRequestModel,
  UpdateFuelPriceConfigurationResponseModel,
} from '../models/fuel-price-configuration.models';

@Component({
  selector: 'app-fuel-price-configuration',
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
  templateUrl: './fuel-price-configuration.page.html',
})
export class FuelPriceConfigurationPage {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(FuelPriceConfigurationService);
  private readonly notificationService = inject(NotificationService);

  protected readonly configForm: FormGroup = this.formBuilder.group({
    gasoline: [0, [Validators.required, Validators.min(1)]],
    gas: [0, [Validators.required, Validators.min(1)]],
    diesel: [0, [Validators.required, Validators.min(1)]],
    alcohol: [0, [Validators.required, Validators.min(1)]],
  });

  get gasolineControl() {
    return this.configForm.get('gasoline');
  }
  get gasControl() {
    return this.configForm.get('gas');
  }
  get dieselControl() {
    return this.configForm.get('diesel');
  }
  get alcoholControl() {
    return this.configForm.get('alcohol');
  }

  protected readonly config$ = this.route.data.pipe(
    filter((data) => !!data['config']),
    map((data) => data['config'] as GetFuelPriceConfigurationResponseModel),
    tap((config) =>
      this.configForm.patchValue({
        gasoline: config.gasoline,
        gas: config.gas,
        diesel: config.diesel,
        alcohol: config.alcohol,
      }),
    ),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public save(): void {
    if (this.configForm.invalid) {
      this.configForm.markAllAsTouched();
      return;
    }

    const request: UpdateFuelPriceConfigurationRequestModel = {
      gasoline: this.configForm.value.gasoline,
      gas: this.configForm.value.gas,
      diesel: this.configForm.value.diesel,
      alcohol: this.configForm.value.alcohol,
    };

    const observer: PartialObserver<UpdateFuelPriceConfigurationResponseModel> = {
      next: () => {
        this.notificationService.success('Configuração de preços atualizada com sucesso!');
        this.router.navigate(['/inicio']);
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
    };

    this.service.updateConfiguration(request).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/inicio']);
  }
}
