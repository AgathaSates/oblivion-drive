import { Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AsShellComponent } from './components/shared/shell/as-shell.component';
import { MatIcon } from '@angular/material/icon';
import { AuthService } from './components/modules/auth/services/auth.service';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-root',
  imports: [AsShellComponent, RouterOutlet, MatIcon, AsyncPipe],
  templateUrl: './app.html',
})
export class App {
  protected readonly router = inject(Router);

  private readonly authService = inject(AuthService);
  protected readonly accessToken$ = this.authService.accessToken$;

  public logout(): void {
    this.authService.logout().subscribe(() => {
      this.router.navigate(['/auth/login']);
    });
  }
}
