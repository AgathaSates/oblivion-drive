import { AsyncPipe, CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take, tap } from 'rxjs/operators';

import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  GetBillingPlanByIdResponseModel,
  UpdateBillingPlanRequestModel,
  UpdateBillingPlanResponseModel,
} from '../../models/billing-plan.models';
import { BillingPlanService } from '../../services/billing-plan.service';
import { VehicleGroupDetailModel } from '../../../vehicle-groups/models/vehicle-group.models';

@Component({
  selector: 'app-billing-plan-edit',
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
  templateUrl: './billing-plan-edit.page.html',
})
export class BillingPlanEditPage {
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
    filter((data) => !!data['vehicleGroups']),
    map((data) => data['vehicleGroups'] as VehicleGroupDetailModel[]),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected readonly billingPlan$ = this.route.data.pipe(
    filter((data) => !!data['billingPlan']),
    map((data) => data['billingPlan'] as GetBillingPlanByIdResponseModel),
    tap((plan) =>
      this.form.patchValue({
        name: plan.name,
        vehicleGroupId: plan.vehicleGroupId,
        dailyPlanDailyRate: plan.dailyPlanDailyRate,
        dailyPlanPricePerKilometer: plan.dailyPlanPricePerKilometer,
        controlledPlanDailyRate: plan.controlledPlanDailyRate,
        controlledPlanExtraPricePerKilometer: plan.controlledPlanExtraPricePerKilometer,
        freePlanDailyRate: plan.freePlanDailyRate,
      }),
    ),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: UpdateBillingPlanRequestModel = {
      name: this.form.value.name,
      vehicleGroupId: this.form.value.vehicleGroupId,
      dailyPlanDailyRate: this.form.value.dailyPlanDailyRate,
      dailyPlanPricePerKilometer: this.form.value.dailyPlanPricePerKilometer,
      controlledPlanDailyRate: this.form.value.controlledPlanDailyRate,
      controlledPlanExtraPricePerKilometer: this.form.value.controlledPlanExtraPricePerKilometer,
      freePlanDailyRate: this.form.value.freePlanDailyRate,
    };

    const updateObserver: PartialObserver<UpdateBillingPlanResponseModel> = {
      next: () => {
        this.notificationService.success('Plano de cobrança atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/planos-de-cobranca']);
      },
    };

    this.billingPlan$
      .pipe(
        take(1),
        switchMap((plan) => this.billingPlanService.updateBillingPlan(plan.id, request)),
      )
      .subscribe(updateObserver);
  }

  public goBack(): void {
    this.router.navigate(['/planos-de-cobranca']);
  }
}
