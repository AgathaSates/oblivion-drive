import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  DeleteDriverResponseModel,
  DriverDetailModel,
  GetAllDriversResponseModel,
  GetDriverByIdResponseModel,
  RegisterDriverRequestModel,
  RegisterDriverResponseModel,
  UpdateDriverRequestModel,
  UpdateDriverResponseModel,
} from '../models/driver.models';
import { environment } from '../../../../../environments/environment';

@Injectable()
export class DriverService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl: string = `${environment.apiUrl}/api/drivers`;

  public registerDriver(
    requestModel: RegisterDriverRequestModel,
  ): Observable<RegisterDriverResponseModel> {
    return this.http.post<RegisterDriverResponseModel>(this.apiUrl, requestModel);
  }

  public updateDriver(
    driverId: string,
    requestModel: UpdateDriverRequestModel,
  ): Observable<UpdateDriverResponseModel> {
    const url: string = `${this.apiUrl}/${driverId}`;
    return this.http.put<UpdateDriverResponseModel>(url, requestModel);
  }

  public deleteDriver(driverId: string): Observable<DeleteDriverResponseModel> {
    const url: string = `${this.apiUrl}/${driverId}`;
    return this.http.delete<DeleteDriverResponseModel>(url);
  }

  public getDriverById(driverId: string): Observable<GetDriverByIdResponseModel> {
    const url: string = `${this.apiUrl}/${driverId}`;
    return this.http.get<GetDriverByIdResponseModel>(url);
  }

  public getAllDrivers(): Observable<DriverDetailModel[]> {
    return this.http
      .get<GetAllDriversResponseModel>(this.apiUrl)
      .pipe(map((response) => response.drivers));
  }
}
