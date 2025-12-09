import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { PartnerService } from '../../services/partner.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { DeletePartnerResponseModel, PartnerDetailModel } from '../../models/partner.models';

@Component({
  selector: 'app-partner-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule],
  templateUrl: './partner-delete.page.html',
})
export class PartnerDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly partnerService = inject(PartnerService);
  private readonly notificationService = inject(NotificationService);

  protected readonly partner$ = this.route.data.pipe(
    filter((data) => !!data['partner']),
    map((data) => data['partner'] as PartnerDetailModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeletePartnerResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Parceiro excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o parceiro.');
        }
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
      complete: () => {
        this.router.navigate(['/parceiros']);
      },
    };

    this.partner$
      .pipe(
        take(1),
        switchMap((partner) => this.partnerService.deletePartner(partner.id)),
      )
      .subscribe(deleteObserver);
  }

  public goBack(): void {
    this.router.navigate(['/parceiros']);
  }
}
