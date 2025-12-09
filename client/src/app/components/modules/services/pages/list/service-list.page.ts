import { AsyncPipe, CurrencyPipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

import { filter, map } from 'rxjs';
import { ServiceModel, ChargeTypeModel } from '../../models/service.models';

@Component({
  selector: 'app-service-list',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    SlicePipe,
    CurrencyPipe,
  ],
  templateUrl: './service-list.page.html',
})
export class ServiceListPage {
  private readonly route = inject(ActivatedRoute);

  protected readonly services$ = this.route.data.pipe(
    filter((data) => data['services']),
    map((data) => data['services'] as ServiceModel[]),
  );

  protected readonly chargeTypeLabelMap: Record<ChargeTypeModel, string> = {
    [ChargeTypeModel.Fixed]: 'Valor fixo',
    [ChargeTypeModel.PerDay]: 'Cobrança por dia',
  };

  protected getChargeTypeLabel(type: ChargeTypeModel): string {
    return this.chargeTypeLabelMap[type] ?? type;
  }
}
