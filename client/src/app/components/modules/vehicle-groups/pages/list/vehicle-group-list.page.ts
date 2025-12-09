import { AsyncPipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

import { filter, map } from 'rxjs';
import { VehicleGroupDetailModel } from '../../models/vehicle-group.models';

@Component({
  selector: 'app-vehicle-group-list',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    SlicePipe,
  ],
  templateUrl: './vehicle-group-list.page.html',
})
export class VehicleGroupListPage {
  private readonly route = inject(ActivatedRoute);

  protected readonly vehicleGroups$ = this.route.data.pipe(
    filter((data) => !!data['vehicleGroups']),
    map((data) => data['vehicleGroups'] as VehicleGroupDetailModel[]),
  );
}
