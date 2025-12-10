import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  VehicleDetailModel,
  GetAllVehiclesResponseModel,
  GetVehicleByIdResponseModel,
  RegisterVehicleRequestModel,
  RegisterVehicleResponseModel,
  UpdateVehicleRequestModel,
  UpdateVehicleResponseModel,
  DeleteVehicleResponseModel,
} from '../models/vehicle.models';
import { environment } from '../../../../../environments/environment';

@Injectable()
export class VehicleService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/vehicles`;

  public registerVehicle(
    requestModel: RegisterVehicleRequestModel,
  ): Observable<RegisterVehicleResponseModel> {
    return this.http.post<RegisterVehicleResponseModel>(this.apiUrl, requestModel);
  }

  public updateVehicle(
    vehicleId: string,
    requestModel: UpdateVehicleRequestModel,
  ): Observable<UpdateVehicleResponseModel> {
    const url = `${this.apiUrl}/${vehicleId}`;
    return this.http.put<UpdateVehicleResponseModel>(url, requestModel);
  }

  public deleteVehicle(vehicleId: string): Observable<DeleteVehicleResponseModel> {
    const url = `${this.apiUrl}/${vehicleId}`;
    return this.http.delete<DeleteVehicleResponseModel>(url);
  }

  public getVehicleById(vehicleId: string): Observable<GetVehicleByIdResponseModel> {
    const url = `${this.apiUrl}/${vehicleId}`;
    return this.http.get<GetVehicleByIdResponseModel>(url);
  }

  public getAllVehicles(vehicleGroupId?: string): Observable<VehicleDetailModel[]> {
    let params = new HttpParams();

    if (vehicleGroupId) {
      params = params.set('vehicleGroupId', vehicleGroupId);
    }

    return this.http
      .get<GetAllVehiclesResponseModel>(this.apiUrl, { params })
      .pipe(map((response) => response.vehicles));
  }
}
