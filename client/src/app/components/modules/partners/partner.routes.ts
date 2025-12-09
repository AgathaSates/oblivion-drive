import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import { PartnerDetailModel, GetPartnerByIdResponseModel } from './models/partner.models';

import { PartnerService } from './services/partner.service';
import { PartnerDeletePage } from './pages/delete/partner-delete.page';
import { PartnerEditPage } from './pages/edit/partner-edit.page';
import { PartnerListPage } from './pages/list/partner-list.page';
import { PartnerCreatePage } from './pages/register/partner-create.page';

export const partnersListResolver: ResolveFn<PartnerDetailModel[]> = () => {
  const partnerService = inject(PartnerService);
  return partnerService.getAllPartners();
};

export const partnerDetailsResolver: ResolveFn<GetPartnerByIdResponseModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const partnerService = inject(PartnerService);

  const partnerId = route.paramMap.get('id');
  if (!partnerId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return partnerService.getPartnerById(partnerId);
};

export const partnerRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: PartnerListPage,
        resolve: { partners: partnersListResolver },
      },
      {
        path: 'cadastrar',
        component: PartnerCreatePage,
      },
      {
        path: 'editar/:id',
        component: PartnerEditPage,
        resolve: { partner: partnerDetailsResolver },
      },
      {
        path: 'excluir/:id',
        component: PartnerDeletePage,
        resolve: { partner: partnerDetailsResolver },
      },
    ],
    providers: [PartnerService],
  },
];
