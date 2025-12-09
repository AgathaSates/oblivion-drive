import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';

import {
  VehicleGroupDetailModel,
  GetAllVehicleGroupsResponseModel,
  GetVehicleGroupByIdResponseModel,
  RegisterVehicleGroupRequestModel,
  RegisterVehicleGroupResponseModel,
  UpdateVehicleGroupRequestModel,
  UpdateVehicleGroupResponseModel,
  DeleteVehicleGroupResponseModel,
} from '../models/vehicle-group.models';
import { environment } from '../../../../../environments/environment';

@Injectable()
export class VehicleGroupService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/vehicle-groups`;

  public registerVehicleGroup(
    requestModel: RegisterVehicleGroupRequestModel,
  ): Observable<RegisterVehicleGroupResponseModel> {
    return this.http.post<RegisterVehicleGroupResponseModel>(this.apiUrl, requestModel);
  }

  public updateVehicleGroup(
    vehicleGroupId: string,
    requestModel: UpdateVehicleGroupRequestModel,
  ): Observable<UpdateVehicleGroupResponseModel> {
    const fullUrl = `${this.apiUrl}/${vehicleGroupId}`;
    return this.http.put<UpdateVehicleGroupResponseModel>(fullUrl, requestModel);
  }

  public deleteVehicleGroup(vehicleGroupId: string): Observable<DeleteVehicleGroupResponseModel> {
    const fullUrl = `${this.apiUrl}/${vehicleGroupId}`;
    return this.http.delete<DeleteVehicleGroupResponseModel>(fullUrl);
  }

  public getVehicleGroupById(vehicleGroupId: string): Observable<GetVehicleGroupByIdResponseModel> {
    const fullUrl = `${this.apiUrl}/${vehicleGroupId}`;
    return this.http.get<GetVehicleGroupByIdResponseModel>(fullUrl);
  }

  public getAllVehicleGroups(): Observable<VehicleGroupDetailModel[]> {
    return this.http
      .get<GetAllVehicleGroupsResponseModel>(this.apiUrl)
      .pipe(map((response) => response.vehicleGroups));
  }
}
