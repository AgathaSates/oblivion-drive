import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { PartialObserver, filter, map, shareReplay, tap } from 'rxjs';

import {
  RegisterBillingPlanRequestModel,
  RegisterBillingPlanResponseModel,
} from '../../models/billing-plan.models';
import { BillingPlanService } from '../../services/billing-plan.service';
import { VehicleGroupDetailModel } from '../../../vehicle-groups/models/vehicle-group.models';
import { NotificationService } from '../../../../shared/notification/notification.service';

@Component({
  selector: 'app-billing-plan-create',
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
  ],
  templateUrl: './billing-plan-create.page.html',
})
export class BillingPlanCreatePage {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly billingPlanService = inject(BillingPlanService);
  private readonly notificationService = inject(NotificationService);

  protected readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    vehicleGroupId: ['', [Validators.required]],
    dailyPlanDailyRate: [null, [Validators.required, Validators.min(0)]],
    dailyPlanPricePerKilometer: [null, [Validators.required, Validators.min(0)]],
    controlledPlanDailyRate: [null, [Validators.required, Validators.min(0)]],
    controlledPlanExtraPricePerKilometer: [null, [Validators.required, Validators.min(0)]],
    freePlanDailyRate: [null, [Validators.required, Validators.min(0)]],
  });

  get nameControl() {
    return this.form.get('name');
  }

  get vehicleGroupIdControl() {
    return this.form.get('vehicleGroupId');
  }

  get dailyPlanDailyRateControl() {
    return this.form.get('dailyPlanDailyRate');
  }

  get dailyPlanPricePerKilometerControl() {
    return this.form.get('dailyPlanPricePerKilometer');
  }

  get controlledPlanDailyRateControl() {
    return this.form.get('controlledPlanDailyRate');
  }

  get controlledPlanExtraPricePerKilometerControl() {
    return this.form.get('controlledPlanExtraPricePerKilometer');
  }

  get freePlanDailyRateControl() {
    return this.form.get('freePlanDailyRate');
  }

  protected readonly vehicleGroups$ = this.route.data.pipe(
    map((data) => (data['vehicleGroups'] as VehicleGroupDetailModel[]) ?? []),
    tap((vehicleGroups) => {
      if (!vehicleGroups || vehicleGroups.length === 0) {
        this.notificationService.warning(
          'Não é possível cadastrar plano de cobrança sem categorias de veículos cadastradas.',
        );
        this.router.navigate(['/categorias']);
      }
    }),
    filter((vehicleGroups) => !!vehicleGroups && vehicleGroups.length > 0),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: RegisterBillingPlanRequestModel = {
      name: this.form.value.name,
      vehicleGroupId: this.form.value.vehicleGroupId,
      dailyPlanDailyRate: this.form.value.dailyPlanDailyRate,
      dailyPlanPricePerKilometer: this.form.value.dailyPlanPricePerKilometer,
      controlledPlanDailyRate: this.form.value.controlledPlanDailyRate,
      controlledPlanExtraPricePerKilometer: this.form.value.controlledPlanExtraPricePerKilometer,
      freePlanDailyRate: this.form.value.freePlanDailyRate,
    };

    const observer: PartialObserver<RegisterBillingPlanResponseModel> = {
      next: (response) => {
        this.notificationService.success(
          `O plano de cobrança "${response?.name ?? request.name}" foi cadastrado com sucesso!`,
        );
        this.router.navigate(['/planos-de-cobranca']);
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
    };

    this.billingPlanService.registerBillingPlan(request).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/planos-de-cobranca']);
  }
}
