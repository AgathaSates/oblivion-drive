import { CurrencyPipe } from '@angular/common';
import { Component, Inject, ViewEncapsulation } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import {
  CompleteRentalReturnRequestModel,
  RentalReturnConfirmationDialogResult,
} from '../../../models/rental.models';

export interface RentalReturnConfirmationDialogRentalInfo {
  driverName: string;
  vehicleLabel: string;
  planTypeLabel: string;
  estimatedRentalAmount: number;
}

export interface RentalReturnConfirmationDialogData {
  rental: RentalReturnConfirmationDialogRentalInfo;
  request: CompleteRentalReturnRequestModel;
}

@Component({
  selector: 'app-rental-return-confirmation-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule, CurrencyPipe],
  template: `
    <h2 mat-dialog-title>Confirmar devolução</h2>

    <mat-dialog-content class="pt-1">
      <p class="mb-2"><strong>Você está prestes a concluir a devolução.</strong></p>

      <div class="mb-2">
        <p class="mb-1"><strong>Condutor:</strong> {{ data.rental.driverName }}</p>
        <p class="mb-1"><strong>Veículo:</strong> {{ data.rental.vehicleLabel }}</p>
        <p class="mb-1"><strong>Plano:</strong> {{ data.rental.planTypeLabel }}</p>
      </div>

      <div class="mb-2">
        <p class="mb-1"><strong>Data devolução:</strong> {{ data.request.actualReturnDate }}</p>
        <p class="mb-1">
          <strong>KM:</strong> {{ data.request.initialOdometerInKm }} →
          {{ data.request.currentOdometerInKm }}
        </p>
        <p class="mb-1">
          <strong>Tanque cheio:</strong> {{ data.request.isFuelTankFullOnReturn ? 'Sim' : 'Não' }}
        </p>
        <p class="mb-1"><strong>Danos:</strong> {{ data.request.hasDamage ? 'Sim' : 'Não' }}</p>
        @if (data.request.couponName) {
          <p class="mb-1"><strong>Cupom:</strong> {{ data.request.couponName }}</p>
        }
      </div>

      <div class="mb-1">
        <p class="mb-0">
          <strong>Valor estimado:</strong>
          {{ data.rental.estimatedRentalAmount | currency: 'BRL' : 'symbol-narrow' }}
        </p>
        <small class="text-muted"> O valor final será calculado ao confirmar. </small>
      </div>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancel()">Cancelar</button>
      <button mat-flat-button color="primary" type="button" (click)="confirm()">Confirmar</button>
    </mat-dialog-actions>
  `,
  styleUrls: ['./rental-return-confirmation-dialog.scss'],
  encapsulation: ViewEncapsulation.None,
})
export class RentalReturnConfirmationDialogComponent {
  constructor(
    private readonly dialogRef: MatDialogRef<
      RentalReturnConfirmationDialogComponent,
      RentalReturnConfirmationDialogResult
    >,
    @Inject(MAT_DIALOG_DATA) public readonly data: RentalReturnConfirmationDialogData,
  ) {}

  public confirm(): void {
    this.dialogRef.close({ confirmed: true });
  }

  public cancel(): void {
    this.dialogRef.close({ confirmed: false });
  }
}
