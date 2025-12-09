import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { NotificationService } from '../../../../shared/notification/notification.service';
import {
  RegisterPartnerRequestModel,
  RegisterPartnerResponseModel,
} from '../../models/partner.models';
import { PartialObserver } from 'rxjs';
import { PartnerService } from '../../services/partner.service';

@Component({
  selector: 'app-partner-create',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './partner-create.page.html',
})
export class PartnerCreatePage {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly partnersService = inject(PartnerService);
  private readonly notificationService = inject(NotificationService);

  protected readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
  });

  get nameControl() {
    return this.form.get('name');
  }

  public submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: RegisterPartnerRequestModel = {
      name: this.form.value.name,
    };

    const observer: PartialObserver<RegisterPartnerResponseModel> = {
      next: (response) => {
        this.notificationService.success(
          `O parceiro "${response?.name ?? request.name}" foi cadastrado com sucesso!`,
        );
        this.router.navigate(['/parceiros']);
      },
      error: (err) => {
        this.notificationService.error(err.error);
      },
    };

    this.partnersService.registerPartner(request).subscribe(observer);
  }

  public goBack(): void {
    this.router.navigate(['/parceiros']);
  }
}
