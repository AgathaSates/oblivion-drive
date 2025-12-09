import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import {
  DeleteServiceResponseModel,
  GetAllServicesResponseModel,
  GetServiceByIdResponseModel,
  RegisterServiceRequestModel,
  RegisterServiceResponseModel,
  ServiceModel,
  UpdateServiceRequestModel,
  UpdateServiceResponseModel,
} from './models/service.models';
import { environment } from '../../../../environments/environment';

@Injectable()
export class ServicesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/services`;

  public registerService(
    requestModel: RegisterServiceRequestModel,
  ): Observable<RegisterServiceResponseModel> {
    const fullUrl = this.apiUrl;

    return this.http.post<RegisterServiceResponseModel>(fullUrl, requestModel);
  }

  public updateService(
    serviceId: string,
    requestModel: UpdateServiceRequestModel,
  ): Observable<UpdateServiceResponseModel> {
    const fullUrl = `${this.apiUrl}/${serviceId}`;

    return this.http.put<UpdateServiceResponseModel>(fullUrl, requestModel);
  }

  public deleteService(serviceId: string): Observable<DeleteServiceResponseModel> {
    const fullUrl = `${this.apiUrl}/${serviceId}`;

    return this.http.delete<DeleteServiceResponseModel>(fullUrl);
  }

  public getServiceById(serviceId: string): Observable<GetServiceByIdResponseModel> {
    const fullUrl = `${this.apiUrl}/${serviceId}`;

    return this.http.get<GetServiceByIdResponseModel>(fullUrl);
  }

  public getAllServices(): Observable<ServiceModel[]> {
    return this.http
      .get<GetAllServicesResponseModel>(this.apiUrl)
      .pipe(map((response) => response.services));
  }
}
