import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  ClientDetailModel,
  DeleteClientResponseModel,
  GetAllClientsResponseModel,
  GetClientByIdResponseModel,
  RegisterClientRequestModel,
  RegisterClientResponseModel,
  UpdateClientRequestModel,
  UpdateClientResponseModel,
} from '../models/client.models';
import { environment } from '../../../../../environments/environment';

@Injectable()
export class ClientService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl: string = `${environment.apiUrl}/api/clients`;

  public registerClient(
    requestModel: RegisterClientRequestModel,
  ): Observable<RegisterClientResponseModel> {
    return this.http.post<RegisterClientResponseModel>(this.apiUrl, requestModel);
  }

  public updateClient(
    clientId: string,
    requestModel: UpdateClientRequestModel,
  ): Observable<UpdateClientResponseModel> {
    const url: string = `${this.apiUrl}/${clientId}`;
    return this.http.put<UpdateClientResponseModel>(url, requestModel);
  }

  public deleteClient(clientId: string): Observable<DeleteClientResponseModel> {
    const url: string = `${this.apiUrl}/${clientId}`;
    return this.http.delete<DeleteClientResponseModel>(url);
  }

  public getClientById(clientId: string): Observable<GetClientByIdResponseModel> {
    const url: string = `${this.apiUrl}/${clientId}`;
    return this.http.get<GetClientByIdResponseModel>(url);
  }

  public getAllClients(): Observable<ClientDetailModel[]> {
    return this.http
      .get<GetAllClientsResponseModel>(this.apiUrl)
      .pipe(map((response) => response.clients));
  }
}
