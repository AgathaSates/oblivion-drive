import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import {
  BillingPlanDetailModel,
  GetBillingPlanByIdResponseModel,
} from './models/billing-plan.models';

import { BillingPlanService } from './services/billing-plan.service';

// Dependência: grupos de veículos (categorias)
import { VehicleGroupDetailModel } from '../vehicle-groups/models/vehicle-group.models';
import { VehicleGroupService } from '../vehicle-groups/services/vehicle-group.service';
import { BillingPlanListPage } from './pages/list/billing-plan-list.page';
import { BillingPlanCreatePage } from './pages/register/billing-plan-create.page';
import { BillingPlanEditPage } from './pages/edit/billing-plan-edit.page';
import { BillingPlanDeletePage } from './pages/delete/billing-plan-delete.page';

export const billingPlansListResolver: ResolveFn<BillingPlanDetailModel[]> = () => {
  const billingPlanService = inject(BillingPlanService);
  return billingPlanService.getAllBillingPlans();
};

export const billingPlanDetailsResolver: ResolveFn<GetBillingPlanByIdResponseModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const billingPlanService = inject(BillingPlanService);

  const billingPlanId = route.paramMap.get('id');
  if (!billingPlanId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return billingPlanService.getBillingPlanById(billingPlanId);
};

export const vehicleGroupsListResolver: ResolveFn<VehicleGroupDetailModel[]> = () => {
  const vehicleGroupService = inject(VehicleGroupService);
  return vehicleGroupService.getAllVehicleGroups();
};

export const billingPlanRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: BillingPlanListPage,
        resolve: {
          billingPlans: billingPlansListResolver,
          vehicleGroups: vehicleGroupsListResolver,
        },
      },
      {
        path: 'cadastrar',
        component: BillingPlanCreatePage,
        resolve: {
          vehicleGroups: vehicleGroupsListResolver,
        },
      },
      {
        path: 'editar/:id',
        component: BillingPlanEditPage,
        resolve: {
          billingPlan: billingPlanDetailsResolver,
          vehicleGroups: vehicleGroupsListResolver,
        },
      },
      {
        path: 'excluir/:id',
        component: BillingPlanDeletePage,
        resolve: {
          billingPlan: billingPlanDetailsResolver,
        },
      },
    ],
    providers: [BillingPlanService, VehicleGroupService],
  },
];
