// -------------------- Enum ------------------------

export enum RentalPlanTypeModel {
  Daily = 'Daily',
  Controlled = 'Controlled',
  Free = 'Free',
}

// -------------------- Detalhe de aluguel ------------------------

export interface RentalDetailModel {
  id: string;
  clientId: string;
  driverId: string;
  vehicleId: string;
  planType: RentalPlanTypeModel;
  startDate: string;
  expectedReturnDate: string;
  actualReturnDate: string | null;
  estimatedRentalAmount: number;
  grossRentalAmount: number;
  finalAmountToPay: number;
  isCompleted: boolean;

  couponId: string | null;
  serviceIds: string[];
}

export type RentalModel = RentalDetailModel;

// -------------------- Listagem ------------------------

export interface GetAllRentalsResponseModel {
  rentals: RentalDetailModel[];
}

// -------------------- Detalhe ------------------------

export interface GetRentalByIdResponseModel {
  rental: RentalDetailModel;
}

// -------------------- Cadastro ------------------------

export interface RegisterRentalRequestModel {
  clientId: string;
  driverId: string;
  vehicleId: string;
  planType: RentalPlanTypeModel;
  startDate: string;
  expectedReturnDate: string;
  insuranceDailyPricePerPerson: number;
  insurancePersonsCount: number;
  estimatedTotalKilometers?: number | null;
  serviceIds?: string[] | null;
}

export interface RegisterRentalResponseModel {
  createdSuccessfully: boolean;
  rentalId: string;
  estimatedRentalAmount: number;
}

// -------------------- Edição ------------------------

export interface UpdateRentalRequestModel {
  clientId: string;
  driverId: string;
  vehicleId: string;
  planType: RentalPlanTypeModel;
  startDate: string;
  expectedReturnDate: string;
  insuranceDailyPricePerPerson: number;
  insurancePersonsCount: number;
  estimatedTotalKilometers?: number | null;
  serviceIds?: string[] | null;
}

export interface UpdateRentalResponseModel {
  updatedSuccessfully: boolean;
  rentalId: string;
  estimatedRentalAmount: number;
}

// -------------------- Exclusão ------------------------

export interface DeleteRentalResponseModel {
  deletedSuccessfully: boolean;
  rentalId: string;
}

// -------------------- Devolução ------------------------

export interface CompleteRentalReturnRequestModel {
  actualReturnDate: string;
  initialOdometerInKm: number;
  currentOdometerInKm: number;
  isFuelTankFullOnReturn: boolean;
  hasDamage: boolean;
  couponName?: string | null;
}

export interface CompleteRentalReturnResponseModel {
  completedSuccessfully: boolean;
  rentalId: string;
  grossRentalAmount: number;
  finalAmountToPay: number;
  couponId: string | null;
  couponDiscountAmount: number;
}

export interface SendRentalReceiptEmailRequestModel {
  email: string;
}

export interface SendRentalReceiptEmailResponseModel {
  sentSuccessfully: boolean;
}

export interface RentalReturnConfirmationDialogResult {
  confirmed: boolean;
}
