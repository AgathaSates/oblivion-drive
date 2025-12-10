import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { DriverService } from '../../services/driver.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { DeleteDriverResponseModel, GetDriverByIdResponseModel } from '../../models/driver.models';

@Component({
  selector: 'app-driver-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule],
  templateUrl: './driver-delete.page.html',
})
export class DriverDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly driverService = inject(DriverService);
  private readonly notificationService = inject(NotificationService);

  protected readonly driver$ = this.route.data.pipe(
    filter((data) => !!data['driver']),
    map((data) => data['driver'] as GetDriverByIdResponseModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeleteDriverResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Condutor excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o condutor.');
        }
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/condutores']);
      },
    };

    this.driver$
      .pipe(
        take(1),
        switchMap((driver) => this.driverService.deleteDriver(driver.id)),
      )
      .subscribe(deleteObserver);
  }

  public goBack(): void {
    this.router.navigate(['/condutores']);
  }
}
