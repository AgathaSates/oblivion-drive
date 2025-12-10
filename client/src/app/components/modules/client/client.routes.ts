import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn, Routes } from '@angular/router';

import { ClientDetailModel, GetClientByIdResponseModel } from './models/client.models';

import { ClientService } from './services/client.service';
import { ClientListPage } from './pages/list/client-list.page';
import { ClientCreatePage } from './pages/register/client-create.page';
import { ClientDeletePage } from './pages/delete/client-delete.page';
import { ClientEditPage } from './pages/edit/client-edit.page';

export const clientsListResolver: ResolveFn<ClientDetailModel[]> = () => {
  const clientService = inject(ClientService);
  return clientService.getAllClients();
};

export const clientDetailsResolver: ResolveFn<GetClientByIdResponseModel> = (
  route: ActivatedRouteSnapshot,
) => {
  const clientService = inject(ClientService);

  const clientId: string | null = route.paramMap.get('id');
  if (!clientId) {
    throw new Error('Route parameter "id" was not provided.');
  }

  return clientService.getClientById(clientId);
};

export const clientRoutes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: ClientListPage,
        resolve: { clients: clientsListResolver },
      },
      {
        path: 'cadastrar',
        component: ClientCreatePage,
      },
      {
        path: 'editar/:id',
        component: ClientEditPage,
        resolve: { client: clientDetailsResolver },
      },
      {
        path: 'excluir/:id',
        component: ClientDeletePage,
        resolve: { client: clientDetailsResolver },
      },
    ],
    providers: [ClientService],
  },
];
