import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import { RentalDetailModel } from './models/rental.models';
import { RentalService } from './services/rental.service';

import { ClientDetailModel } from '../client/models/client.models';
import { ClientService } from '../client/services/client.service';

import { DriverDetailModel } from '../driver/models/driver.models';
import { DriverService } from '../driver/services/driver.service';

import { VehicleDetailModel } from '../vehicle/models/vehicle.models';
import { VehicleService } from '../vehicle/services/vehicle.service';

import { ServiceDetailModel } from '../services/models/service.models';
import { ServicesService } from '../services/Services.service';

import { VehicleGroupDetailModel } from '../vehicle-groups/models/vehicle-group.models';
import { VehicleGroupService } from '../vehicle-groups/services/vehicle-group.service';

import { RentalListPage } from './pages/list/rental-list.page';
import { RentalCreatePage } from './pages/register/rental-create.page';
import { RentalEditPage } from './pages/edit/rental-edit.page';
import { RentalDeletePage } from './pages/delete/rental-delete.page';
import { RentalReturnPage } from './return/rental-return.page';

export const rentalsListResolver: ResolveFn<RentalDetailModel[]> = () => {
  const rentalService = inject(RentalService);
  return rentalService.getAllRentals();
};

export const rentalDetailsResolver: ResolveFn<RentalDetailModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const rentalService = inject(RentalService);

  const rentalId = route.paramMap.get('id');
  if (!rentalId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return rentalService.getRentalById(rentalId);
};

export const clientsListResolver: ResolveFn<ClientDetailModel[]> = () => {
  const clientService = inject(ClientService);
  return clientService.getAllClients();
};

export const driversListResolver: ResolveFn<DriverDetailModel[]> = () => {
  const driverService = inject(DriverService);
  return driverService.getAllDrivers();
};

export const vehiclesListResolver: ResolveFn<VehicleDetailModel[]> = () => {
  const vehicleService = inject(VehicleService);
  return vehicleService.getAllVehicles();
};

export const servicesListResolver: ResolveFn<ServiceDetailModel[]> = () => {
  const servicesService = inject(ServicesService);
  return servicesService.getAllServices();
};

export const vehicleGroupsListResolver: ResolveFn<VehicleGroupDetailModel[]> = () => {
  const vehicleGroupService = inject(VehicleGroupService);
  return vehicleGroupService.getAllVehicleGroups();
};

export const rentalRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: RentalListPage,
        resolve: {
          rentals: rentalsListResolver,
        },
      },
      {
        path: 'cadastrar',
        component: RentalCreatePage,
        resolve: {
          clients: clientsListResolver,
          drivers: driversListResolver,
          vehicles: vehiclesListResolver,
          services: servicesListResolver,
          vehicleGroups: vehicleGroupsListResolver,
        },
      },
      {
        path: 'editar/:id',
        component: RentalEditPage,
        resolve: {
          rental: rentalDetailsResolver,
          clients: clientsListResolver,
          drivers: driversListResolver,
          vehicles: vehiclesListResolver,
          services: servicesListResolver,
          vehicleGroups: vehicleGroupsListResolver,
        },
      },
      {
        path: 'excluir/:id',
        component: RentalDeletePage,
        resolve: {
          rental: rentalDetailsResolver,
        },
      },
      {
        path: 'devolver/:id',
        component: RentalReturnPage,
        resolve: {
          rental: rentalDetailsResolver,
        },
      },
    ],
    providers: [
      RentalService,
      ClientService,
      DriverService,
      VehicleService,
      ServicesService,
      VehicleGroupService,
    ],
  },
];
