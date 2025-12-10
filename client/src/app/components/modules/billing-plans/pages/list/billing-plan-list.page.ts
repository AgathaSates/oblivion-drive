import { AsyncPipe, SlicePipe, CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

import { combineLatest, map } from 'rxjs';

import {
  BillingPlanDetailModel,
  BillingPlanListItemViewModel,
} from '../../models/billing-plan.models';
import { VehicleGroupDetailModel } from '../../../vehicle-groups/models/vehicle-group.models';

@Component({
  selector: 'app-billing-plan-list',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    SlicePipe,
    CurrencyPipe,
  ],
  templateUrl: './billing-plan-list.page.html',
})
export class BillingPlanListPage {
  private readonly route = inject(ActivatedRoute);

  private readonly billingPlansFromRoute$ = this.route.data.pipe(
    map((data) => (data['billingPlans'] as BillingPlanDetailModel[]) ?? []),
  );

  private readonly vehicleGroupsFromRoute$ = this.route.data.pipe(
    map((data) => (data['vehicleGroups'] as VehicleGroupDetailModel[]) ?? []),
  );

  protected readonly billingPlans$ = combineLatest([
    this.billingPlansFromRoute$,
    this.vehicleGroupsFromRoute$,
  ]).pipe(
    map(([billingPlans, vehicleGroups]) =>
      billingPlans.map<BillingPlanListItemViewModel>((plan) => {
        const group = vehicleGroups.find((g) => g.id === plan.vehicleGroupId);

        return {
          ...plan,
          vehicleGroupName: group?.name ?? plan.vehicleGroupId,
        };
      }),
    ),
  );
}
