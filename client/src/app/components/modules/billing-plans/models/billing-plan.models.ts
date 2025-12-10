// -------------------- Listagem ------------------------

export interface BillingPlanDetailModel {
  id: string;
  name: string;
  vehicleGroupId: string;
  dailyPlanDailyRate: number;
  dailyPlanPricePerKilometer: number;
  controlledPlanDailyRate: number;
  controlledPlanExtraPricePerKilometer: number;
  freePlanDailyRate: number;
}

export interface GetAllBillingPlansResponseModel {
  quantity: number;
  billingPlans: BillingPlanDetailModel[];
}

export interface BillingPlanListItemViewModel extends BillingPlanDetailModel {
  vehicleGroupName: string;
}

// -------------------- Cadastro ------------------------

export interface RegisterBillingPlanRequestModel {
  name: string;
  vehicleGroupId: string;
  dailyPlanDailyRate: number;
  dailyPlanPricePerKilometer: number;
  controlledPlanDailyRate: number;
  controlledPlanExtraPricePerKilometer: number;
  freePlanDailyRate: number;
}

export interface RegisterBillingPlanResponseModel {
  createdSuccessfully: boolean;
  name: string;
  vehicleGroupId: string;
  dailyPlanDailyRate: number;
  dailyPlanPricePerKilometer: number;
  controlledPlanDailyRate: number;
  controlledPlanExtraPricePerKilometer: number;
  freePlanDailyRate: number;
}

// -------------------- Edição ------------------------

export interface UpdateBillingPlanRequestModel {
  name: string;
  vehicleGroupId: string;
  dailyPlanDailyRate: number;
  dailyPlanPricePerKilometer: number;
  controlledPlanDailyRate: number;
  controlledPlanExtraPricePerKilometer: number;
  freePlanDailyRate: number;
}

export interface UpdateBillingPlanResponseModel {
  updatedSuccessfully: boolean;
  name: string;
  vehicleGroupId: string;
  dailyPlanDailyRate: number;
  dailyPlanPricePerKilometer: number;
  controlledPlanDailyRate: number;
  controlledPlanExtraPricePerKilometer: number;
  freePlanDailyRate: number;
}

// -------------------- Detalhes ------------------------

export interface GetBillingPlanByIdResponseModel {
  id: string;
  name: string;
  vehicleGroupId: string;
  dailyPlanDailyRate: number;
  dailyPlanPricePerKilometer: number;
  controlledPlanDailyRate: number;
  controlledPlanExtraPricePerKilometer: number;
  freePlanDailyRate: number;
}

// -------------------- Exclusão ------------------------

export interface DeleteBillingPlanResponseModel {
  deletedSuccessfully: boolean;
  billingPlanId: string;
}
