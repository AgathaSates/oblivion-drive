import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { CouponService } from '../../services/coupon.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { DeleteCouponResponseModel, GetCouponByIdResponseModel } from '../../models/coupon.models';

@Component({
  selector: 'app-coupon-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule],
  templateUrl: './coupon-delete.page.html',
})
export class CouponDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly couponService = inject(CouponService);
  private readonly notificationService = inject(NotificationService);

  protected readonly coupon$ = this.route.data.pipe(
    filter((data) => !!data['coupon']),
    map((data) => data['coupon'] as GetCouponByIdResponseModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeleteCouponResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Cupom excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o cupom.');
        }
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
        switchMap((coupon) => this.couponService.deleteCoupon(coupon.id)),
      )
      .subscribe(deleteObserver);
  }

  public goBack(): void {
    this.router.navigate(['/cupons']);
  }
}
