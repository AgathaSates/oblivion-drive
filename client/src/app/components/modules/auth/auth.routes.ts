import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  {
    path: 'registrar',
    loadComponent: () => import('./pages/register/register').then((m) => m.Register),
  },
  { path: 'login', loadComponent: () => import('./pages/login/login').then((m) => m.Login) },
];
