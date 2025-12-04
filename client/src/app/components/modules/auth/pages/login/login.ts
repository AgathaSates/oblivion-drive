import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { NotificationService } from '../../../../shared/notification/notification.service';
import { AuthService } from '../../services/auth.service';
import { PartialObserver } from 'rxjs';
import { LoginModel, AccessTokenModel } from '../../models/auth.models';

@Component({
  selector: 'app-login',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    RouterLink,
    ReactiveFormsModule,
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  protected readonly formBuilder = inject(FormBuilder);
  protected readonly router = inject(Router);
  protected readonly authService = inject(AuthService);
  protected readonly notificationService = inject(NotificationService);

  private readonly passwordPattern: RegExp =
    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$/;

  protected loginForm: FormGroup = this.formBuilder.group({
    userName: ['', [Validators.required, Validators.minLength(3)]],
    password: [
      '',
      [Validators.required, Validators.minLength(6), Validators.pattern(this.passwordPattern)],
    ],
  });

  get userName() {
    return this.loginForm.get('userName');
  }

  get password() {
    return this.loginForm.get('password');
  }

  public login() {
    if (this.loginForm.invalid) return;

    const loginModel: LoginModel = this.loginForm.value;

    const loginObserver: PartialObserver<AccessTokenModel> = {
      next: () => {
        this.notificationService.success('Login realizado com sucesso!');
        this.router.navigate(['/inicio']);
      },
      error: (err) => this.notificationService.error(err.error),
    };

    this.authService.login(loginModel).subscribe(loginObserver);
  }
}
