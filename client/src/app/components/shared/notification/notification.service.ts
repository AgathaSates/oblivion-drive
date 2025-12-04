import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable()
export class NotificationService {
  protected readonly snackBar = inject(MatSnackBar);

  public success(message: string): void {
    this.snackBar.open(message, 'OK', {
      panelClass: ['notification-success'],
    });
  }

  public warning(message: string): void {
    this.snackBar.open(message, 'OK', {
      panelClass: ['notification-warning'],
    });
  }

  public error(message: string): void {
    this.snackBar.open(message, 'OK', {
      panelClass: ['notification-error'],
    });
  }
}
