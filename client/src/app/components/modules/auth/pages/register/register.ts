import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { AccessTokenModel, RegisterModel } from '../../models/auth.models';
import { PartialObserver } from 'rxjs';
import { NotificationService } from '../../../../shared/notification/notification.service';

@Component({
  selector: 'register',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    RouterLink,
    ReactiveFormsModule,
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  protected readonly formBuilder = inject(FormBuilder);
  protected readonly router = inject(Router);
  protected readonly authService = inject(AuthService);
  protected readonly notificationService = inject(NotificationService);

  private readonly usernamePattern: RegExp = /^\S+$/;
  private readonly emailPattern: RegExp = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
  private readonly passwordPattern: RegExp =
    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{6,}$/;

  protected registerForm: FormGroup = this.formBuilder.group({
    userName: [
      '',
      [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(100),
        Validators.pattern(this.usernamePattern),
      ],
    ],
    email: [
      '',
      [Validators.required, Validators.maxLength(256), Validators.pattern(this.emailPattern)],
    ],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(6),
        Validators.maxLength(100),
        Validators.pattern(this.passwordPattern),
      ],
    ],
  });

  get userName() {
    return this.registerForm.get('userName');
  }

  get email() {
    return this.registerForm.get('email');
  }

  get password() {
    return this.registerForm.get('password');
  }

  public register() {
    if (this.registerForm.invalid) return;

    const registerModel: RegisterModel = this.registerForm.value;

    const registerObserver: PartialObserver<AccessTokenModel> = {
      next: () => {
        this.notificationService.success('Conta criada com sucesso! Bem-vindo à Oblivion Drive.');

        this.router.navigate(['/inicio']);
      },
      error: (err) => {
        this.notificationService.error(err.error ?? 'Erro ao registrar usuário.');
      },
    };

    this.authService.registro(registerModel).subscribe(registerObserver);
  }
}
