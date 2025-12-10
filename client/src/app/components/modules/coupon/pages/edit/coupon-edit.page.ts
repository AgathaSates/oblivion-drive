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
  GetCouponByIdResponseModel,
  UpdateCouponRequestModel,
  UpdateCouponResponseModel,
} from '../../models/coupon.models';
import { CouponService } from '../../services/coupon.service';
import { PartnerDetailModel } from '../../../partners/models/partner.models';

@Component({
  selector: 'app-coupon-edit',
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
  templateUrl: './coupon-edit.page.html',
})
export class CouponEditPage {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly couponService = inject(CouponService);
  private readonly notificationService = inject(NotificationService);

  protected readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    value: [null, [Validators.required, Validators.min(0.01)]],
    expirationDate: ['', [Validators.required]],
    partnerId: ['', [Validators.required]],
  });

  get nameControl() {
    return this.form.get('name');
  }

  get valueControl() {
    return this.form.get('value');
  }

  get expirationDateControl() {
    return this.form.get('expirationDate');
  }

  get partnerIdControl() {
    return this.form.get('partnerId');
  }

  protected readonly partners$ = this.route.data.pipe(
    filter((data) => !!data['partners']),
    map((data) => data['partners'] as PartnerDetailModel[]),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  protected readonly coupon$ = this.route.data.pipe(
    filter((data) => !!data['coupon']),
    map((data) => data['coupon'] as GetCouponByIdResponseModel),
    tap((coupon) => {
      const expirationValue =
        coupon.expirationDate instanceof Date
          ? coupon.expirationDate.toISOString().substring(0, 10)
          : coupon.expirationDate;

      this.form.patchValue({
        name: coupon.name,
        value: coupon.value,
        expirationDate: expirationValue,
        partnerId: coupon.partnerId,
      });
    }),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: UpdateCouponRequestModel = {
      name: this.form.value.name,
      value: this.form.value.value,
      expirationDate: this.form.value.expirationDate,
      partnerId: this.form.value.partnerId,
    };

    const updateObserver: PartialObserver<UpdateCouponResponseModel> = {
      next: () => {
        this.notificationService.success('Cupom atualizado com sucesso!');
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/cupons']);
      },
    };

    this.coupon$
      .pipe(
        take(1),
        switchMap((coupon) => this.couponService.updateCoupon(coupon.id, request)),
      )
      .subscribe(updateObserver);
  }

  public goBack(): void {
    this.router.navigate(['/cupons']);
  }
}
