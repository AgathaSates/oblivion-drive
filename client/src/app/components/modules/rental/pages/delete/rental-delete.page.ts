import { AsyncPipe, DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take } from 'rxjs/operators';

import { RentalService } from '../../services/rental.service';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { DeleteRentalResponseModel, RentalDetailModel } from '../../models/rental.models';

@Component({
  selector: 'app-rental-delete',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, AsyncPipe, FormsModule, DatePipe],
  templateUrl: './rental-delete.page.html',
})
export class RentalDeletePage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly rentalService = inject(RentalService);
  private readonly notificationService = inject(NotificationService);

  protected readonly rental$ = this.route.data.pipe(
    filter((data) => !!data['rental']),
    map((data) => data['rental'] as RentalDetailModel),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public confirmDelete(): void {
    const deleteObserver: PartialObserver<DeleteRentalResponseModel> = {
      next: (response) => {
        if (response?.deletedSuccessfully) {
          this.notificationService.success('Aluguel excluído com sucesso!');
        } else {
          this.notificationService.warning('Não foi possível excluir o aluguel.');
        }
      },
      error: (err) => {
        this.notificationService.error(err?.error ?? 'Erro ao excluir aluguel.');
      },
      complete: () => {
        this.router.navigate(['/alugueis']);
      },
    };

    this.rental$
      .pipe(
        take(1),
        switchMap((rental) => this.rentalService.deleteRental(rental.id)),
      )
      .subscribe(deleteObserver);
  }

  public goBack(): void {
    this.router.navigate(['/alugueis']);
  }
}
