import { inject } from '@angular/core';
import { ResolveFn, Routes } from '@angular/router';

import { GetFuelPriceConfigurationResponseModel } from './models/fuel-price-configuration.models';

import { FuelPriceConfigurationService } from './services/fuel-price-configuration.service';
import { FuelPriceConfigurationPage } from './pages/fuel-price-configuration.page';

export const fuelPriceConfigurationResolver: ResolveFn<
  GetFuelPriceConfigurationResponseModel
> = () => {
  const fuelPriceConfigurationService = inject(FuelPriceConfigurationService);
  return fuelPriceConfigurationService.getConfiguration();
};

export const fuelPriceConfigurationRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: FuelPriceConfigurationPage,
        resolve: { config: fuelPriceConfigurationResolver },
      },
    ],
    providers: [FuelPriceConfigurationService],
  },
];
