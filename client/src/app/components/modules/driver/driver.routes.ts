import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import { DriverDetailModel, GetDriverByIdResponseModel } from './models/driver.models';

import { DriverService } from './services/driver.service';
import { ClientDetailModel } from '../client/models/client.models';
import { ClientService } from '../client/services/client.service';
import { DriverDeletePage } from './pages/delete/driver-delete.page';
import { DriverEditPage } from './pages/edit/driver-edit.page';
import { DriverListPage } from './pages/list/driver-list.page';
import { DriverCreatePage } from './pages/register/driver-create.page';

export const driversListResolver: ResolveFn<DriverDetailModel[]> = () => {
  const driverService = inject(DriverService);
  return driverService.getAllDrivers();
};

export const driverDetailsResolver: ResolveFn<GetDriverByIdResponseModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const driverService = inject(DriverService);

  const driverId: string | null = route.paramMap.get('id');
  if (!driverId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return driverService.getDriverById(driverId);
};

export const clientsListResolver: ResolveFn<ClientDetailModel[]> = () => {
  const clientService = inject(ClientService);
  return clientService.getAllClients();
};

export const driverRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: DriverListPage,
        resolve: { drivers: driversListResolver, clients: clientsListResolver },
      },
      {
        path: 'cadastrar',
        component: DriverCreatePage,
        resolve: { clients: clientsListResolver },
      },
      {
        path: 'editar/:id',
        component: DriverEditPage,
        resolve: {
          driver: driverDetailsResolver,
          clients: clientsListResolver,
        },
      },
      {
        path: 'excluir/:id',
        component: DriverDeletePage,
        resolve: { driver: driverDetailsResolver },
      },
    ],
    providers: [DriverService, ClientService],
  },
];
