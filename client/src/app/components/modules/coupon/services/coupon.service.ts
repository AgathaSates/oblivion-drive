import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  CouponDetailModel,
  GetAllCouponsResponseModel,
  GetCouponByIdResponseModel,
  RegisterCouponRequestModel,
  RegisterCouponResponseModel,
  UpdateCouponRequestModel,
  UpdateCouponResponseModel,
  DeleteCouponResponseModel,
} from '../models/coupon.models';
import { environment } from '../../../../../environments/environment';

@Injectable()
export class CouponService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/coupons`;

  public registerCoupon(
    requestModel: RegisterCouponRequestModel,
  ): Observable<RegisterCouponResponseModel> {
    return this.http.post<RegisterCouponResponseModel>(this.apiUrl, requestModel);
  }

  public updateCoupon(
    couponId: string,
    requestModel: UpdateCouponRequestModel,
  ): Observable<UpdateCouponResponseModel> {
    const url = `${this.apiUrl}/${couponId}`;
    return this.http.put<UpdateCouponResponseModel>(url, requestModel);
  }

  public deleteCoupon(couponId: string): Observable<DeleteCouponResponseModel> {
    const url = `${this.apiUrl}/${couponId}`;
    return this.http.delete<DeleteCouponResponseModel>(url);
  }

  public getCouponById(couponId: string): Observable<GetCouponByIdResponseModel> {
    const url = `${this.apiUrl}/${couponId}`;
    return this.http.get<GetCouponByIdResponseModel>(url);
  }

  public getAllCoupons(): Observable<CouponDetailModel[]> {
    return this.http
      .get<GetAllCouponsResponseModel>(this.apiUrl)
      .pipe(map((response) => response.coupons));
  }
}
