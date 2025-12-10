import { AsyncPipe, DatePipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

import { combineLatest, map } from 'rxjs';

import { DriverDetailModel } from '../../models/driver.models';
import { PhoneNumberFormatPipe } from '../../../client/pages/list/phone-number-format.pipe';
import { ClientDetailModel } from '../../../client/models/client.models';

export interface DriverListItemViewModel extends DriverDetailModel {
  clientName: string;
}

@Component({
  selector: 'app-driver-list',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    SlicePipe,
    DatePipe,
    PhoneNumberFormatPipe,
  ],
  templateUrl: './driver-list.page.html',
})
export class DriverListPage {
  private readonly route = inject(ActivatedRoute);

  private readonly driversFromRoute$ = this.route.data.pipe(
    map((data) => (data['drivers'] as DriverDetailModel[]) ?? []),
  );

  private readonly clientsFromRoute$ = this.route.data.pipe(
    map((data) => (data['clients'] as ClientDetailModel[]) ?? []),
  );

  protected readonly drivers$ = combineLatest([
    this.driversFromRoute$,
    this.clientsFromRoute$,
  ]).pipe(
    map(([drivers, clients]) =>
      drivers.map<DriverListItemViewModel>((driver) => {
        const client = clients.find((c) => c.id === driver.clientId);

        return {
          ...driver,
          clientName: client?.name ?? driver.clientId,
        };
      }),
    ),
  );
}
