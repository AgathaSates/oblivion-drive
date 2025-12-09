// -------------------- Listagem ------------------------

export interface GetAllEmployeesForCompanyResponseModel {
  quantity: number;
  employees: EmployeeDetailModel[];
}

export interface EmployeeDetailModel {
  id: string;
  name: string;
  hireDate: string | Date;
  salary: number;
}

// -------------------- Cadastro ------------------------

export interface RegisterEmployeeRequestModel {
  userName: string;
  email: string;
  password: string;
  name: string;
  hireDate: string | Date;
  salary: number;
}

export interface RegisterEmployeeResponseModel {
  createdSuccessfully: boolean;
  name: string;
  userName: string;
}

// -------------------- Edição pela empresa ------------------------

export interface UpdateEmployeeByCompanyRequestModel {
  name: string;
  hireDate: string | Date;
  salary: number;
}

export interface UpdateEmployeeByCompanyResponseModel {
  updatedSuccessfully: boolean;
  name: string;
  hireDate: string | Date;
  salary: number;
}

// -------------------- Edição do próprio perfil ------------------------

export interface UpdateOwnEmployeeRequestModel {
  name: string;
}

export interface UpdateOwnEmployeeResponseModel {
  updatedSuccessfully: boolean;
  name: string;
}

// -------------------- Detalhes ------------------------

export type GetEmployeeByCompanyResponseModel = EmployeeDetailModel;

// -------------------- Exclusão ------------------------

export interface DeleteEmployeeByCompanyResponseModel {
  deletedSuccessfully: boolean;
  employeeId: string;
}
