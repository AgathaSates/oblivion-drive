import { Component, ViewEncapsulation, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface RentalSendReceiptEmailDialogData {
  defaultEmail?: string | null;
}

export interface RentalSendReceiptEmailDialogResult {
  email: string;
}

@Component({
  selector: 'app-rental-send-receipt-email-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  template: `
    <h2 mat-dialog-title class="mb-2">Enviar recibo por e-mail</h2>

    <mat-dialog-content class="pt-2">
      <form [formGroup]="form" class="w-100">
        <mat-form-field appearance="outline" class="w-100">
          <mat-label>E-mail do destinatário</mat-label>
          <input
            matInput
            type="email"
            formControlName="email"
            placeholder="Exemplo: destino@gmail.com"
          />

          @if (emailControl.touched && emailControl.invalid) {
            <mat-error>
              @if (emailControl.errors?.['required']) {
                Informe um e-mail.
              }
              @if (emailControl.errors?.['email']) {
                Informe um e-mail válido.
              }
            </mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>

      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="confirm()">
        Enviar
      </button>
    </mat-dialog-actions>
  `,
  encapsulation: ViewEncapsulation.None,
})
export class RentalSendReceiptEmailDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<RentalSendReceiptEmailDialogComponent>);
  private readonly data = inject<RentalSendReceiptEmailDialogData>(MAT_DIALOG_DATA);

  protected readonly form = new FormGroup({
    email: new FormControl<string>(this.data.defaultEmail ?? '', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
  });

  protected get emailControl(): FormControl<string> {
    return this.form.controls.email;
  }

  protected confirm(): void {
    this.form.markAllAsTouched();

    if (this.form.invalid) return;

    const result: RentalSendReceiptEmailDialogResult = {
      email: this.emailControl.value.trim(),
    };

    this.dialogRef.close(result);
  }
}
