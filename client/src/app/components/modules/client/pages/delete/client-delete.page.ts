import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { ClientService } from '../../services/client.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { ClientDetailModel, DeleteClientResponseModel } from '../../models/client.models';

@Component({
  selector: 'app-client-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule],
  templateUrl: './client-delete.page.html',
})
export class ClientDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly clientService = inject(ClientService);
  private readonly notificationService = inject(NotificationService);

  protected readonly client$ = this.route.data.pipe(
    filter((data) => !!data['client']),
    map((data) => data['client'] as ClientDetailModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeleteClientResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Cliente excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o cliente.');
        }
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/clientes']);
      },
    };

    this.client$
      .pipe(
        take(1),
        switchMap((client) => this.clientService.deleteClient(client.id)),
      )
      .subscribe(deleteObserver);
  }

  public goBack(): void {
    this.router.navigate(['/clientes']);
  }
}
