import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import { EmployeeDetailModel, GetEmployeeByCompanyResponseModel } from './models/employee.models';
import { EmployeeService } from './services/employee.service';
import { EmployeeListPage } from './pages/list/employee-list.page';
import { RegisterEmployeePage } from './pages/register/register-employee.page';
import { EmployeeEditPage } from './pages/edit/employee-edit.page';
import { EmployeeDeletePage } from './pages/delete/employee-delete.page';

export const employeesListResolver: ResolveFn<EmployeeDetailModel[]> = () => {
  const employeeService = inject(EmployeeService);

  return employeeService.getAllEmployeesForCompany();
};

export const employeeDetailsResolver: ResolveFn<GetEmployeeByCompanyResponseModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const employeeService = inject(EmployeeService);

  const employeeId = route.paramMap.get('id');
  if (!employeeId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return employeeService.getEmployeeByIdForCompany(employeeId);
};

export const employeeRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: EmployeeListPage,
        resolve: { employees: employeesListResolver },
      },
      {
        path: 'cadastrar',
        component: RegisterEmployeePage,
      },
      {
        path: 'editar/:id',
        component: EmployeeEditPage,
        resolve: { employee: employeeDetailsResolver },
      },
      {
        path: 'excluir/:id',
        component: EmployeeDeletePage,
        resolve: { employee: employeeDetailsResolver },
      },
    ],
    providers: [EmployeeService],
  },
];
