export interface FuelPriceConfigurationModel {
  gasoline: number;
  gas: number;
  diesel: number;
  alcohol: number;
  lastUpdate: string | Date;
}

// -------------------- Get ------------------------

export type GetFuelPriceConfigurationResponseModel = FuelPriceConfigurationModel;

// -------------------- Update ------------------------

export interface UpdateFuelPriceConfigurationRequestModel {
  gasoline: number;
  gas: number;
  diesel: number;
  alcohol: number;
}

export interface UpdateFuelPriceConfigurationResponseModel {
  gasoline: number;
  gas: number;
  diesel: number;
  alcohol: number;
  lastUpdate: string | Date;
}
