import { AsyncPipe, CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router } from '@angular/router';
import { PartialObserver } from 'rxjs';
import { filter, map, shareReplay, switchMap, take, tap } from 'rxjs/operators';

import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  PartnerDetailModel,
  UpdatePartnerRequestModel,
  UpdatePartnerResponseModel,
} from '../../models/partner.models';
import { PartnerService } from '../../services/partner.service';

@Component({
  selector: 'app-partner-edit',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
    AsyncPipe,
    CommonModule,
  ],
  templateUrl: './partner-edit.page.html',
})
export class PartnerEditPage {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly partnersService = inject(PartnerService);
  private readonly notificationService = inject(NotificationService);

  protected readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
  });

  get nameControl() {
    return this.form.get('name');
  }

  protected readonly partner$ = this.route.data.pipe(
    filter((data) => !!data['partner']),
    map((data) => data['partner'] as PartnerDetailModel),
    tap((partner) =>
      this.form.patchValue({
        name: partner.name,
      }),
    ),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  public save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: UpdatePartnerRequestModel = {
      name: this.form.value.name,
    };

    const updateObserver: PartialObserver<UpdatePartnerResponseModel> = {
      next: () => {
        this.notificationService.success('Parceiro atualizado com sucesso!');
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
        switchMap((partner) => this.partnersService.updatePartner(partner.id, request)),
      )
      .subscribe(updateObserver);
  }

  public goBack(): void {
    this.router.navigate(['/parceiros']);
  }
}
