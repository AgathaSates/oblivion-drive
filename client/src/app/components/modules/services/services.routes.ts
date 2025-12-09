import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import { GetServiceByIdResponseModel, ServiceModel } from './models/service.models';

import { ServiceListPage } from './pages/list/service-list.page';
import { ServicesService } from './Services.service';
import { ServiceCreatePage } from './pages/register/service-create.page';
import { ServiceEditPage } from './pages/edit/service-edit.page';
import { ServiceDeletePage } from './pages/delete/service-delete.page';

export const servicesListResolver: ResolveFn<ServiceModel[]> = () => {
  const servicesService = inject(ServicesService);

  return servicesService.getAllServices();
};

export const serviceDetailsResolver: ResolveFn<GetServiceByIdResponseModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const servicesService = inject(ServicesService);

  const serviceId = route.paramMap.get('id');
  if (!serviceId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return servicesService.getServiceById(serviceId);
};

export const servicesRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: ServiceListPage,
        resolve: { services: servicesListResolver },
      },
      {
        path: 'cadastrar',
        component: ServiceCreatePage,
      },
      {
        path: 'editar/:id',
        component: ServiceEditPage,
        resolve: { service: serviceDetailsResolver },
      },
      {
        path: 'excluir/:id',
        component: ServiceDeletePage,
        resolve: { service: serviceDetailsResolver },
      },
    ],
    providers: [ServicesService],
  },
];
