import {
  ApplicationConfig,
  inject,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { CanActivateFn, provideRouter, Router, Routes } from '@angular/router';
import { provideAuth } from './components/modules/auth/models/auth.provider';
import { provideNotifications } from './components/shared/notification/notification.provider';
import { take, map } from 'rxjs';
import { AuthService } from './components/modules/auth/services/auth.service';
import { EmployeeService } from './components/modules/employee/services/employee.service';

const UnknownUserGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.accessToken$.pipe(
    take(1),
    map((token) => (!token ? true : router.createUrlTree(['/inicio']))),
  );
};

const AuthenticatedUserGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.accessToken$.pipe(
    take(1),
    map((token) => (token ? true : router.createUrlTree(['/auth/login']))),
  );
};

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'auth/login',
    pathMatch: 'full',
  },
  {
    path: 'inicio',
    loadComponent: () => import('./components/home/pages/as-home/as-home').then((m) => m.AsHome),
    canActivate: [AuthenticatedUserGuard],
  },
  {
    path: 'auth',
    loadChildren: () => import('./components/modules/auth/auth.routes').then((m) => m.authRoutes),
    canActivate: [UnknownUserGuard],
  },
  {
    path: 'funcionarios',
    loadChildren: () =>
      import('./components/modules/employee/employee.routes').then((m) => m.employeeRoutes),
    canActivate: [AuthenticatedUserGuard],
  },
  {
    path: 'meu-perfil',
    loadComponent: () =>
      import('./components/modules/employee/pages/profile/employee-profile.page').then(
        (m) => m.EmployeeProfilePage,
      ),
    canActivate: [AuthenticatedUserGuard],
    providers: [EmployeeService],
  },
  {
    path: 'servicos',
    loadChildren: () =>
      import('./components/modules/services/services.routes').then((m) => m.servicesRoutes),
    canActivate: [AuthenticatedUserGuard],
  },
  {
    path: 'categorias',
    loadChildren: () =>
      import('./components/modules/vehicle-groups/vehicle-group.routes').then(
        (m) => m.vehicleGroupRoutes,
      ),
    canActivate: [AuthenticatedUserGuard],
  },
  {
    path: 'combustiveis',
    loadChildren: () =>
      import('./components/modules/fuel-price-configuration/fuel-price-configuration.routes').then(
        (m) => m.fuelPriceConfigurationRoutes,
      ),
    canActivate: [AuthenticatedUserGuard],
  },
  {
    path: 'parceiros',
    loadChildren: () =>
      import('./components/modules/partners/partner.routes').then((m) => m.partnerRoutes),
    canActivate: [AuthenticatedUserGuard],
  },
  {
    path: 'cupons',
    loadChildren: () =>
      import('./components/modules/coupon/coupon.routes').then((m) => m.couponRoutes),
    canActivate: [AuthenticatedUserGuard],
  },
  {
    path: 'planos-de-cobranca',
    loadChildren: () =>
      import('./components/modules/billing-plans/billing-plan.routes').then(
        (m) => m.billingPlanRoutes,
      ),
    canActivate: [AuthenticatedUserGuard],
  },
  {
    path: 'veiculos',
    loadChildren: () =>
      import('./components/modules/vehicle/vehicle.routes').then((m) => m.vehicleRoutes),
    canActivate: [AuthenticatedUserGuard],
  },
];

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideNotifications(),
    provideAuth(),
  ],
};
