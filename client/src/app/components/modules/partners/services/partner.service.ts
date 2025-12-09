import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  PartnerDetailModel,
  GetAllPartnersResponseModel,
  GetPartnerByIdResponseModel,
  RegisterPartnerRequestModel,
  RegisterPartnerResponseModel,
  UpdatePartnerRequestModel,
  UpdatePartnerResponseModel,
  DeletePartnerResponseModel,
} from '../models/partner.models';
import { environment } from '../../../../../environments/environment';

@Injectable()
export class PartnerService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/partners`;

  public registerPartner(
    requestModel: RegisterPartnerRequestModel,
  ): Observable<RegisterPartnerResponseModel> {
    return this.http.post<RegisterPartnerResponseModel>(this.apiUrl, requestModel);
  }

  public updatePartner(
    partnerId: string,
    requestModel: UpdatePartnerRequestModel,
  ): Observable<UpdatePartnerResponseModel> {
    const url = `${this.apiUrl}/${partnerId}`;
    return this.http.put<UpdatePartnerResponseModel>(url, requestModel);
  }

  public deletePartner(partnerId: string): Observable<DeletePartnerResponseModel> {
    const url = `${this.apiUrl}/${partnerId}`;
    return this.http.delete<DeletePartnerResponseModel>(url);
  }

  public getPartnerById(partnerId: string): Observable<GetPartnerByIdResponseModel> {
    const url = `${this.apiUrl}/${partnerId}`;
    return this.http.get<GetPartnerByIdResponseModel>(url);
  }

  public getAllPartners(): Observable<PartnerDetailModel[]> {
    return this.http
      .get<GetAllPartnersResponseModel>(this.apiUrl)
      .pipe(map((response) => response.partners));
  }
}
