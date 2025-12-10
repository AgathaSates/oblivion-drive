import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import { VehicleDetailModel, GetVehicleByIdResponseModel } from './models/vehicle.models';

import { VehicleService } from './services/vehicle.service';

import { VehicleGroupDetailModel } from '../vehicle-groups/models/vehicle-group.models';
import { VehicleGroupService } from '../vehicle-groups/services/vehicle-group.service';
import { VehicleListPage } from './pages/list/vehicle-list.page';
import { VehicleCreatePage } from './pages/register/vehicle-create.page';
import { VehicleEditPage } from './pages/edit/vehicle-edit.page';
import { VehicleDeletePage } from './pages/delete/vehicle-delete.page';

export const vehiclesListResolver: ResolveFn<VehicleDetailModel[]> = () => {
  const vehicleService = inject(VehicleService);

  return vehicleService.getAllVehicles();
};

export const vehicleDetailsResolver: ResolveFn<GetVehicleByIdResponseModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const vehicleService = inject(VehicleService);

  const vehicleId = route.paramMap.get('id');
  if (!vehicleId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return vehicleService.getVehicleById(vehicleId);
};

export const vehicleGroupsListResolver: ResolveFn<VehicleGroupDetailModel[]> = () => {
  const vehicleGroupService = inject(VehicleGroupService);
  return vehicleGroupService.getAllVehicleGroups();
};

export const vehicleRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: VehicleListPage,
        resolve: {
          vehicles: vehiclesListResolver,
          vehicleGroups: vehicleGroupsListResolver,
        },
      },
      {
        path: 'cadastrar',
        component: VehicleCreatePage,
        resolve: {
          vehicleGroups: vehicleGroupsListResolver,
        },
      },
      {
        path: 'editar/:id',
        component: VehicleEditPage,
        resolve: {
          vehicle: vehicleDetailsResolver,
          vehicleGroups: vehicleGroupsListResolver,
        },
      },
      {
        path: 'excluir/:id',
        component: VehicleDeletePage,
        resolve: {
          vehicle: vehicleDetailsResolver,
        },
      },
    ],
    providers: [VehicleService, VehicleGroupService],
  },
];
