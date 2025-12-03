import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { AsyncPipe } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { Observable } from 'rxjs';
import { map, shareReplay, take } from 'rxjs/operators';
import { NavbarItem } from '../../../models/navbar-item';
import { NAVBAR_ITEMS } from '../../../data/navbar-items.data';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthenticatedUserModel, UserTypeModel } from '../../modules/auth/models/auth.models';
import { AsConfirmLogoutDialogComponent } from '../as-confirm-logout-dialog/as-confirm-logout-dialog';
import { MatDialog } from '@angular/material/dialog';

@Component({
  selector: 'as-shell',
  templateUrl: './as-shell.component.html',
  styleUrl: './as-shell.component.scss',
  imports: [
    MatToolbarModule,
    MatButtonModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    AsyncPipe,
    RouterLink,
    RouterLinkActive,
  ],
})
export class AsShellComponent {
  private breakpointObserver = inject(BreakpointObserver);
  private readonly dialog = inject(MatDialog);
  private _authenticatedUser!: AuthenticatedUserModel;

  public navbarItems: readonly NavbarItem[] = NAVBAR_ITEMS;

  @Input({ required: true })
  set AuthenticatedUser(value: AuthenticatedUserModel) {
    this._authenticatedUser = value;
    this.updateNavbarItemsForUser();
  }
  get AuthenticatedUser(): AuthenticatedUserModel {
    return this._authenticatedUser;
  }
  @Output() logoutRequested = new EventEmitter<void>();

  public openLogoutDialog(): void {
    const dialogRef = this.dialog.open(AsConfirmLogoutDialogComponent, {
      width: '360px',
      panelClass: 'as-shell-dialog-centered',
    });

    dialogRef
      .afterClosed()
      .pipe(take(1))
      .subscribe((confirmed) => {
        if (confirmed) {
          this.logoutRequested.emit();
        }
      });
  }

  private updateNavbarItemsForUser(): void {
    if (!this._authenticatedUser) {
      this.navbarItems = NAVBAR_ITEMS;
      return;
    }

    const role = this._authenticatedUser.userType;

    this.navbarItems = NAVBAR_ITEMS.filter((item) => {
      if (!item.allowedUserTypes || item.allowedUserTypes.length === 0) {
        return true;
      }
      return item.allowedUserTypes.includes(role);
    });
  }

  public readonly userTypeDisplay: Record<UserTypeModel, string> = {
    [UserTypeModel.Employee]: 'Colaborador',
    [UserTypeModel.Company]: 'Empresa',
  } as const;

  isHandset$: Observable<boolean> = this.breakpointObserver
    .observe([Breakpoints.XSmall, Breakpoints.Small, Breakpoints.Handset])
    .pipe(
      map((result) => result.matches),
      shareReplay(),
    );
}
