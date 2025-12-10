export interface DriverDetailModel {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  cpf: string;
  cnh: string;
  cnhExpirationDate: string;
  clientId: string;
  isClientAlsoDriver: boolean;
}

export interface GetAllDriversResponseModel {
  quantity: number;
  drivers: DriverDetailModel[];
}

export interface RegisterDriverRequestModel {
  name: string;
  email: string;
  phoneNumber: string;
  cpf: string;
  cnh: string;
  cnhExpirationDate: string;
  clientId: string;
  isClientAlsoDriver: boolean;
}

export interface RegisterDriverResponseModel {
  createdSuccessfully: boolean;
  name: string;
  email: string;
  phoneNumber: string;
  cpf: string;
  cnh: string;
  cnhExpirationDate: string;
  clientId: string;
  isClientAlsoDriver: boolean;
}

export interface UpdateDriverRequestModel {
  name: string;
  email: string;
  phoneNumber: string;
  cpf: string;
  cnh: string;
  cnhExpirationDate: string;
  clientId: string;
  isClientAlsoDriver: boolean;
}

export interface UpdateDriverResponseModel {
  updatedSuccessfully: boolean;
  name: string;
  email: string;
  phoneNumber: string;
  cpf: string;
  cnh: string;
  cnhExpirationDate: string;
  clientId: string;
  isClientAlsoDriver: boolean;
}

export interface GetDriverByIdResponseModel {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  cpf: string;
  cnh: string;
  cnhExpirationDate: string;
  clientId: string;
  isClientAlsoDriver: boolean;
}

export interface DeleteDriverResponseModel {
  deletedSuccessfully: boolean;
  driverId: string;
}
