// -------------------- Listagem ------------------------

export interface VehicleGroupDetailModel {
  id: string;
  name: string;
}

export interface GetAllVehicleGroupsResponseModel {
  quantity: number;
  vehicleGroups: VehicleGroupDetailModel[];
}

// -------------------- Cadastro ------------------------

export interface RegisterVehicleGroupRequestModel {
  name: string;
}

export interface RegisterVehicleGroupResponseModel {
  createdSuccessfully: boolean;
  name: string;
}

// -------------------- Edição ------------------------

export interface UpdateVehicleGroupRequestModel {
  name: string;
}

export interface UpdateVehicleGroupResponseModel {
  updatedSuccessfully: boolean;
  name: string;
}

// -------------------- Detalhes ------------------------

export type GetVehicleGroupByIdResponseModel = VehicleGroupDetailModel;

// -------------------- Exclusão ------------------------

export interface DeleteVehicleGroupResponseModel {
  deletedSuccessfully: boolean;
  vehicleGroupId: string;
}
