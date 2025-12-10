import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { BillingPlanService } from '../../services/billing-plan.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  DeleteBillingPlanResponseModel,
  GetBillingPlanByIdResponseModel,
} from '../../models/billing-plan.models';

@Component({
  selector: 'app-billing-plan-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule],
  templateUrl: './billing-plan-delete.page.html',
})
export class BillingPlanDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly billingPlanService = inject(BillingPlanService);
  private readonly notificationService = inject(NotificationService);

  protected readonly billingPlan$ = this.route.data.pipe(
    filter((data) => !!data['billingPlan']),
    map((data) => data['billingPlan'] as GetBillingPlanByIdResponseModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeleteBillingPlanResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Plano de cobrança excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o plano de cobrança.');
        }
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
        switchMap((plan) => this.billingPlanService.deleteBillingPlan(plan.id)),
      )
      .subscribe(deleteObserver);
  }

  public goBack(): void {
    this.router.navigate(['/planos-de-cobranca']);
  }
}
