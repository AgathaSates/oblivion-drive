import { Component, ViewEncapsulation } from '@angular/core';
import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'as-confirm-logout-dialog',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Confirmar logout</h2>

    <mat-dialog-content> Tem certeza que deseja sair da aplicação? </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>

      <button mat-flat-button color="primary" [mat-dialog-close]="true">Sair</button>
    </mat-dialog-actions>
  `,
  styleUrl: './as-confirm-logout-dialog.scss',
  encapsulation: ViewEncapsulation.None,
})
export class AsConfirmLogoutDialogComponent {}
