import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import { CouponDetailModel, GetCouponByIdResponseModel } from './models/coupon.models';
import { PartnerDetailModel } from '../partners/models/partner.models';

import { CouponService } from './services/coupon.service';
import { PartnerService } from '../partners/services/partner.service';
import { CouponListPage } from './pages/list/coupon-list.page';
import { CouponDeletePage } from './pages/delete/coupon-delete.page';
import { CouponEditPage } from './pages/edit/coupon-edit.page';
import { CouponCreatePage } from './pages/register/coupon-create.page';

export const couponsListResolver: ResolveFn<CouponDetailModel[]> = () => {
  const couponService = inject(CouponService);
  return couponService.getAllCoupons();
};

export const couponDetailsResolver: ResolveFn<GetCouponByIdResponseModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const couponService = inject(CouponService);

  const couponId = route.paramMap.get('id');
  if (!couponId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return couponService.getCouponById(couponId);
};

export const partnersListResolver: ResolveFn<PartnerDetailModel[]> = () => {
  const partnerService = inject(PartnerService);
  return partnerService.getAllPartners();
};

export const couponRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: CouponListPage,
        resolve: { coupons: couponsListResolver, partners: partnersListResolver },
      },
      {
        path: 'cadastrar',
        component: CouponCreatePage,
        resolve: { partners: partnersListResolver },
      },
      {
        path: 'editar/:id',
        component: CouponEditPage,
        resolve: {
          coupon: couponDetailsResolver,
          partners: partnersListResolver,
        },
      },
      {
        path: 'excluir/:id',
        component: CouponDeletePage,
        resolve: { coupon: couponDetailsResolver },
      },
    ],
    providers: [CouponService, PartnerService],
  },
];
