import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { map, Observable } from 'rxjs';
import {
  DeleteEmployeeByCompanyResponseModel,
  EmployeeDetailModel,
  GetAllEmployeesForCompanyResponseModel,
  GetEmployeeByCompanyResponseModel,
  RegisterEmployeeRequestModel,
  RegisterEmployeeResponseModel,
  UpdateEmployeeByCompanyRequestModel,
  UpdateEmployeeByCompanyResponseModel,
  UpdateOwnEmployeeRequestModel,
  UpdateOwnEmployeeResponseModel,
} from '../models/employee.models';

@Injectable()
export class EmployeeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl + '/api/employee';

  public registerEmployee(
    requestModel: RegisterEmployeeRequestModel,
  ): Observable<RegisterEmployeeResponseModel> {
    const fullUrl = `${this.apiUrl}/register`;

    return this.http.post<RegisterEmployeeResponseModel>(fullUrl, requestModel);
  }

  public updateEmployeeByCompany(
    employeeId: string,
    requestModel: UpdateEmployeeByCompanyRequestModel,
  ): Observable<UpdateEmployeeByCompanyResponseModel> {
    const fullUrl = `${this.apiUrl}/${employeeId}`;

    return this.http.patch<UpdateEmployeeByCompanyResponseModel>(fullUrl, requestModel);
  }

  public updateOwnEmployeeProfile(
    requestModel: UpdateOwnEmployeeRequestModel,
  ): Observable<UpdateOwnEmployeeResponseModel> {
    const fullUrl = `${this.apiUrl}/profile`;

    return this.http.patch<UpdateOwnEmployeeResponseModel>(fullUrl, requestModel);
  }

  public deleteEmployeeByCompany(
    employeeId: string,
  ): Observable<DeleteEmployeeByCompanyResponseModel> {
    const fullUrl = `${this.apiUrl}/${employeeId}`;
    return this.http.delete<DeleteEmployeeByCompanyResponseModel>(fullUrl);
  }

  public getEmployeeByIdForCompany(
    employeeId: string,
  ): Observable<GetEmployeeByCompanyResponseModel> {
    const fullUrl = `${this.apiUrl}/${employeeId}`;

    return this.http.get<GetEmployeeByCompanyResponseModel>(fullUrl);
  }

  public getAllEmployeesForCompany(): Observable<EmployeeDetailModel[]> {
    return this.http
      .get<GetAllEmployeesForCompanyResponseModel>(this.apiUrl)
      .pipe(map((response) => response.employees));
  }
}
