import { AsyncPipe, DatePipe, SlicePipe, CurrencyPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

import { combineLatest, map } from 'rxjs';
import { CouponDetailModel } from '../../models/coupon.models';
import { PartnerDetailModel } from '../../../partners/models/partner.models';

export interface CouponListItemViewModel extends CouponDetailModel {
  partnerName: string;
}

@Component({
  selector: 'app-coupon-list',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    SlicePipe,
    DatePipe,
    CurrencyPipe,
  ],
  templateUrl: './coupon-list.page.html',
})
export class CouponListPage {
  private readonly route = inject(ActivatedRoute);

  private readonly couponsFromRoute$ = this.route.data.pipe(
    map((data) => (data['coupons'] as CouponDetailModel[]) ?? []),
  );

  private readonly partnersFromRoute$ = this.route.data.pipe(
    map((data) => (data['partners'] as PartnerDetailModel[]) ?? []),
  );

  protected readonly coupons$ = combineLatest([
    this.couponsFromRoute$,
    this.partnersFromRoute$,
  ]).pipe(
    map(([coupons, partners]) =>
      coupons.map<CouponListItemViewModel>((coupon) => {
        const partner = partners.find((p) => p.id === coupon.partnerId);

        return {
          ...coupon,
          partnerName: partner?.name ?? coupon.partnerId,
        };
      }),
    ),
  );
}
