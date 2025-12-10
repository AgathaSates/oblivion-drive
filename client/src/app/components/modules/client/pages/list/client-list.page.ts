import { AsyncPipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

import { filter, map } from 'rxjs';
import { ClientDetailModel, ClientType } from '../../models/client.models';
import { PhoneNumberFormatPipe } from './phone-number-format.pipe';

@Component({
  selector: 'app-client-list',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    SlicePipe,
    PhoneNumberFormatPipe,
  ],
  templateUrl: './client-list.page.html',
})
export class ClientListPage {
  private readonly route = inject(ActivatedRoute);

  protected readonly ClientType = ClientType;

  protected readonly clients$ = this.route.data.pipe(
    filter((data) => data['clients']),
    map((data) => data['clients'] as ClientDetailModel[]),
  );
}
