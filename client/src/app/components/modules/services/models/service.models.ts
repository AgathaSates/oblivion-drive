// -------------------- Enum ------------------------

export enum ChargeTypeModel {
  Fixed = 'Fixed',
  PerDay = 'perDay',
}

export interface ServiceDetailModel {
  id: string;
  name: string;
  price: number;
  chargeType: ChargeTypeModel;
}

export type ServiceModel = ServiceDetailModel;

// -------------------- Listagem ------------------------

export interface GetAllServicesResponseModel {
  quantity: number;
  services: ServiceDetailModel[];
}

// -------------------- Cadastro ------------------------

export interface RegisterServiceRequestModel {
  name: string;
  price: number;
  chargeType: ChargeTypeModel;
}

export interface RegisterServiceResponseModel {
  createdSuccessfully: boolean;
  name: string;
  price: number;
  chargeType: ChargeTypeModel;
}

// -------------------- Edição ------------------------

export interface UpdateServiceRequestModel {
  name: string;
  price: number;
  chargeType: ChargeTypeModel;
}

export interface UpdateServiceResponseModel {
  updatedSuccessfully: boolean;
  name: string;
  price: number;
  chargeType: ChargeTypeModel;
}

// -------------------- Detalhes ------------------------

export type GetServiceByIdResponseModel = ServiceDetailModel;

// -------------------- Exclusão ------------------------

export interface DeleteServiceResponseModel {
  deletedSuccessfully: boolean;
  serviceId: string;
}
