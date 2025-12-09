import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';

import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';
import { PartialObserver } from 'rxjs';

import { NotificationService } from '../../../../shared/notification/notification.service';

import { ServiceDetailModel, DeleteServiceResponseModel } from '../../models/service.models';
import { ServicesService } from '../../Services.service';

@Component({
  selector: 'app-service-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule],
  templateUrl: './service-delete.page.html',
})
export class ServiceDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly serviceService = inject(ServicesService);
  private readonly notificationService = inject(NotificationService);

  protected readonly service$ = this.route.data.pipe(
    filter((data) => !!data['service']),
    map((data) => data['service'] as ServiceDetailModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeleteServiceResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Serviço excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o serviço.');
        }
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/servicos']);
      },
    };

    this.service$
      .pipe(
        take(1),
        switchMap((service) => this.serviceService.deleteService(service.id)),
      )
      .subscribe(deleteObserver);
  }

  public goBack(): void {
    this.router.navigate(['/servicos']);
  }
}
