import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { VehicleGroupService } from '../../services/vehicle-group.service';
import { NotificationService } from '../../../../shared/notification/notification.service';

import {
  DeleteVehicleGroupResponseModel,
  VehicleGroupDetailModel,
} from '../../models/vehicle-group.models';

@Component({
  selector: 'app-vehicle-group-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule],
  templateUrl: './vehicle-group-delete.page.html',
})
export class VehicleGroupDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly vehicleGroupService = inject(VehicleGroupService);
  private readonly notificationService = inject(NotificationService);

  protected readonly vehicleGroup$ = this.route.data.pipe(
    filter((data) => !!data['vehicleGroup']),
    map((data) => data['vehicleGroup'] as VehicleGroupDetailModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeleteVehicleGroupResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Grupo de veículos excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o grupo de veículos.');
        }
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/categorias']);
      },
    };

    this.vehicleGroup$
      .pipe(
        take(1),
        switchMap((vehicleGroup) => this.vehicleGroupService.deleteVehicleGroup(vehicleGroup.id)),
      )
      .subscribe(deleteObserver);
  }

  public goBack(): void {
    this.router.navigate(['/categorias']);
  }
}
