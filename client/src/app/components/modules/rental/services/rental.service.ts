import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import {
  RegisterRentalRequestModel,
  RegisterRentalResponseModel,
  UpdateRentalRequestModel,
  UpdateRentalResponseModel,
  DeleteRentalResponseModel,
  RentalDetailModel,
  GetRentalByIdResponseModel,
  GetAllRentalsResponseModel,
  CompleteRentalReturnRequestModel,
  CompleteRentalReturnResponseModel,
} from '../models/rental.models';

@Injectable()
export class RentalService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl: string = `${environment.apiUrl}/api/rentals`;

  public registerRental(
    requestModel: RegisterRentalRequestModel,
  ): Observable<RegisterRentalResponseModel> {
    const fullUrl: string = this.apiUrl;
    return this.http.post<RegisterRentalResponseModel>(fullUrl, requestModel);
  }

  public updateRental(
    rentalId: string,
    requestModel: UpdateRentalRequestModel,
  ): Observable<UpdateRentalResponseModel> {
    const fullUrl: string = `${this.apiUrl}/${rentalId}`;
    return this.http.put<UpdateRentalResponseModel>(fullUrl, requestModel);
  }

  public deleteRental(rentalId: string): Observable<DeleteRentalResponseModel> {
    const fullUrl: string = `${this.apiUrl}/${rentalId}`;
    return this.http.delete<DeleteRentalResponseModel>(fullUrl);
  }

  public getRentalById(rentalId: string): Observable<RentalDetailModel> {
    const fullUrl: string = `${this.apiUrl}/${rentalId}`;

    return this.http
      .get<GetRentalByIdResponseModel>(fullUrl)
      .pipe(map((response) => response.rental));
  }

  public getAllRentals(quantity?: number): Observable<RentalDetailModel[]> {
    let params = new HttpParams();

    if (quantity !== undefined && quantity !== null) {
      params = params.set('quantity', quantity.toString());
    }

    return this.http
      .get<GetAllRentalsResponseModel>(this.apiUrl, { params })
      .pipe(map((response) => response.rentals));
  }

  public completeRentalReturn(
    rentalId: string,
    requestModel: CompleteRentalReturnRequestModel,
  ): Observable<CompleteRentalReturnResponseModel> {
    const fullUrl: string = `${this.apiUrl}/${rentalId}/return`;
    return this.http.post<CompleteRentalReturnResponseModel>(fullUrl, requestModel);
  }
}
