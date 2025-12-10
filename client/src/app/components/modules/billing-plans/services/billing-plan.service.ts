import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  BillingPlanDetailModel,
  GetAllBillingPlansResponseModel,
  GetBillingPlanByIdResponseModel,
  RegisterBillingPlanRequestModel,
  RegisterBillingPlanResponseModel,
  UpdateBillingPlanRequestModel,
  UpdateBillingPlanResponseModel,
  DeleteBillingPlanResponseModel,
} from '../models/billing-plan.models';
import { environment } from '../../../../../environments/environment';

@Injectable()
export class BillingPlanService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/billing-plans`;

  public registerBillingPlan(
    requestModel: RegisterBillingPlanRequestModel,
  ): Observable<RegisterBillingPlanResponseModel> {
    return this.http.post<RegisterBillingPlanResponseModel>(this.apiUrl, requestModel);
  }

  public updateBillingPlan(
    billingPlanId: string,
    requestModel: UpdateBillingPlanRequestModel,
  ): Observable<UpdateBillingPlanResponseModel> {
    const url = `${this.apiUrl}/${billingPlanId}`;
    return this.http.put<UpdateBillingPlanResponseModel>(url, requestModel);
  }

  public deleteBillingPlan(billingPlanId: string): Observable<DeleteBillingPlanResponseModel> {
    const url = `${this.apiUrl}/${billingPlanId}`;
    return this.http.delete<DeleteBillingPlanResponseModel>(url);
  }

  public getBillingPlanById(billingPlanId: string): Observable<GetBillingPlanByIdResponseModel> {
    const url = `${this.apiUrl}/${billingPlanId}`;
    return this.http.get<GetBillingPlanByIdResponseModel>(url);
  }

  public getAllBillingPlans(): Observable<BillingPlanDetailModel[]> {
    return this.http
      .get<GetAllBillingPlansResponseModel>(this.apiUrl)
      .pipe(map((response) => response.billingPlans));
  }
}
