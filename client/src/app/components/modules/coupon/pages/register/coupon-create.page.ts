import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import { PartialObserver, filter, map, shareReplay, tap } from 'rxjs';

import {
  RegisterCouponRequestModel,
  RegisterCouponResponseModel,
} from '../../models/coupon.models';
import { CouponService } from '../../services/coupon.service';
import { PartnerDetailModel } from '../../../partners/models/partner.models';
import { NotificationService } from '../../../../shared/notification/notification.service';

@Component({
  selector: 'app-coupon-create',
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
  templateUrl: './coupon-create.page.html',
})
export class CouponCreatePage {
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
    map((data) => (data['partners'] as PartnerDetailModel[]) ?? []),
    tap((partners) => {
      if (!partners || partners.length === 0) {
        this.notificationService.warning(
          'Não é possível cadastrar cupom sem parceiros cadastrados.',
        );
        this.router.navigate(['/parceiros']);
      }
    }),
    filter((partners) => !!partners && partners.length > 0),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: RegisterCouponRequestModel = {
      name: this.form.value.name,
      value: this.form.value.value,
      expirationDate: this.form.value.expirationDate,
      partnerId: this.form.value.partnerId,
    };

    const observer: PartialObserver<RegisterCouponResponseModel> = {
      next: (response) => {
        this.notificationService.success(
          `O cupom "${response?.name ?? request.name}" foi cadastrado com sucesso!`,
        );
        this.router.navigate(['/cupons']);
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
    };

    this.couponService.registerCoupon(request).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/cupons']);
  }
}
