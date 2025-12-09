import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import {
  GetFuelPriceConfigurationResponseModel,
  UpdateFuelPriceConfigurationRequestModel,
  UpdateFuelPriceConfigurationResponseModel,
} from '../models/fuel-price-configuration.models';

@Injectable()
export class FuelPriceConfigurationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/fuel-price-configuration`;

  public getConfiguration(): Observable<GetFuelPriceConfigurationResponseModel> {
    return this.http.get<GetFuelPriceConfigurationResponseModel>(this.apiUrl);
  }

  public updateConfiguration(
    requestModel: UpdateFuelPriceConfigurationRequestModel,
  ): Observable<UpdateFuelPriceConfigurationResponseModel> {
    return this.http.put<UpdateFuelPriceConfigurationResponseModel>(this.apiUrl, requestModel);
  }
}
