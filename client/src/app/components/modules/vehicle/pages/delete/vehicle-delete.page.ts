import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { VehicleService } from '../../services/vehicle.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  DeleteVehicleResponseModel,
  GetVehicleByIdResponseModel,
} from '../../models/vehicle.models';

@Component({
  selector: 'app-vehicle-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule],
  templateUrl: './vehicle-delete.page.html',
})
export class VehicleDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly vehicleService = inject(VehicleService);
  private readonly notificationService = inject(NotificationService);

  protected readonly vehicle$ = this.route.data.pipe(
    filter((data) => !!data['vehicle']),
    map((data) => data['vehicle'] as GetVehicleByIdResponseModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeleteVehicleResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Veículo excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o veículo.');
        }
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/veiculos']);
      },
    };

    this.vehicle$
      .pipe(
        take(1),
        switchMap((vehicle) => this.vehicleService.deleteVehicle(vehicle.id)),
      )
      .subscribe(deleteObserver);
  }

  public goBack(): void {
    this.router.navigate(['/veiculos']);
  }
}
