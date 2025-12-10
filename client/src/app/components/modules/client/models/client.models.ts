export enum ClientType {
  Individual = 1,
  LegalEntity = 2,
}

export interface ClientDetailModel {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  clientType: ClientType;
  cpf: string | null;
  rg: string | null;
  cnh: string | null;
  cnpj: string | null;
  state: string;
  city: string;
  district: string;
  street: string;
  number: string;
}

export interface GetAllClientsResponseModel {
  quantity: number;
  clients: ClientDetailModel[];
}

export interface RegisterClientRequestModel {
  name: string;
  email: string;
  phoneNumber: string;
  clientType: ClientType;
  cpf?: string | null;
  rg?: string | null;
  cnh?: string | null;
  cnpj?: string | null;
  state: string;
  city: string;
  district: string;
  street: string;
  number: string;
}

export interface RegisterClientResponseModel {
  createdSuccessfully: boolean;
  name: string;
  email: string;
  phoneNumber: string;
  clientType: ClientType;
  cpf: string | null;
  rg: string | null;
  cnh: string | null;
  cnpj: string | null;
  state: string;
  city: string;
  district: string;
  street: string;
  number: string;
}

export interface UpdateClientRequestModel {
  name: string;
  email: string;
  phoneNumber: string;
  clientType: ClientType;
  cpf?: string | null;
  rg?: string | null;
  cnh?: string | null;
  cnpj?: string | null;
  state: string;
  city: string;
  district: string;
  street: string;
  number: string;
}

export interface UpdateClientResponseModel {
  updatedSuccessfully: boolean;
  name: string;
  email: string;
  phoneNumber: string;
  clientType: ClientType;
  cpf: string | null;
  rg: string | null;
  cnh: string | null;
  cnpj: string | null;
  state: string;
  city: string;
  district: string;
  street: string;
  number: string;
}

export interface GetClientByIdResponseModel {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  clientType: ClientType;
  cpf: string | null;
  rg: string | null;
  cnh: string | null;
  cnpj: string | null;
  state: string;
  city: string;
  district: string;
  street: string;
  number: string;
}

export interface DeleteClientResponseModel {
  deletedSuccessfully: boolean;
  clientId: string;
}
