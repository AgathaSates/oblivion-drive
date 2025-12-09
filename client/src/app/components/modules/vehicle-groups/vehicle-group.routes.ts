import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import {
  VehicleGroupDetailModel,
  GetVehicleGroupByIdResponseModel,
} from './models/vehicle-group.models';
import { VehicleGroupService } from './services/vehicle-group.service';
import { VehicleGroupListPage } from './pages/list/vehicle-group-list.page';
import { VehicleGroupCreatePage } from './pages/register/vehicle-group-create.page';
import { VehicleGroupEditPage } from './pages/edit/vehicle-group-edit.page';
import { VehicleGroupDeletePage } from './pages/delete/vehicle-group-delete.page';

export const vehicleGroupListResolver: ResolveFn<VehicleGroupDetailModel[]> = () => {
  const service = inject(VehicleGroupService);

  return service.getAllVehicleGroups();
};

export const vehicleGroupDetailsResolver: ResolveFn<GetVehicleGroupByIdResponseModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const service = inject(VehicleGroupService);

  const vehicleGroupId = route.paramMap.get('id');
  if (!vehicleGroupId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return service.getVehicleGroupById(vehicleGroupId);
};

export const vehicleGroupRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: VehicleGroupListPage,
        resolve: { vehicleGroups: vehicleGroupListResolver },
      },
      {
        path: 'cadastrar',
        component: VehicleGroupCreatePage,
      },
      {
        path: 'editar/:id',
        component: VehicleGroupEditPage,
        resolve: { vehicleGroup: vehicleGroupDetailsResolver },
      },
      {
        path: 'excluir/:id',
        component: VehicleGroupDeletePage,
        resolve: { vehicleGroup: vehicleGroupDetailsResolver },
      },
    ],
    providers: [VehicleGroupService],
  },
];
