// -------------------- Enum FuelType (espelhando o backend) --------------------

export enum FuelType {
  Gasoline = 0,
  Gas = 1,
  Diesel = 2,
  Alcohol = 3,
}

export const FuelTypeLabels: Record<FuelType, string> = {
  [FuelType.Gasoline]: 'Gasolina',
  [FuelType.Gas]: 'GNV',
  [FuelType.Diesel]: 'Diesel',
  [FuelType.Alcohol]: 'Álcool',
};

// -------------------- Listagem ------------------------

export interface VehicleDetailModel {
  id: string;
  licensePlate: string;
  brand: string;
  model: string;
  color: string;
  fuelType: FuelType;
  fuelTankCapacityInLiters: number;
  year: number;
  vehicleGroupId: string;
  photoBytes: string;
}

export interface GetAllVehiclesResponseModel {
  quantity: number;
  vehicles: VehicleDetailModel[];
}

// -------------------- Cadastro ------------------------

export interface RegisterVehicleRequestModel {
  licensePlate: string;
  brand: string;
  model: string;
  color: string;
  fuelType: FuelType;
  fuelTankCapacityInLiters: number;
  year: number;
  vehicleGroupId: string;
  photoBytes: string;
}

export interface RegisterVehicleResponseModel {
  createdSuccessfully: boolean;
  licensePlate: string;
  brand: string;
  model: string;
  color: string;
  fuelType: FuelType;
  fuelTankCapacityInLiters: number;
  year: number;
  vehicleGroupId: string;
  photoBytes: string;
}

// -------------------- Edição ------------------------

export interface UpdateVehicleRequestModel {
  brand: string;
  model: string;
  color: string;
  fuelType: FuelType;
  fuelTankCapacityInLiters: number;
  year: number;
  vehicleGroupId: string;
  photoBytes: string | null;
}

export interface UpdateVehicleResponseModel {
  updatedSuccessfully: boolean;
  licensePlate: string;
  brand: string;
  model: string;
  color: string;
  fuelType: FuelType;
  fuelTankCapacityInLiters: number;
  year: number;
  vehicleGroupId: string;
  photoBytes: string;
}

// -------------------- Detalhes ------------------------

export interface GetVehicleByIdResponseModel {
  id: string;
  licensePlate: string;
  brand: string;
  model: string;
  color: string;
  fuelType: FuelType;
  fuelTankCapacityInLiters: number;
  year: number;
  vehicleGroupId: string;
  photoBytes: string;
}

// -------------------- Exclusão ------------------------

export interface DeleteVehicleResponseModel {
  deletedSuccessfully: boolean;
  vehicleId: string;
}
