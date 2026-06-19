import { Component, inject } from '@angular/core';
import { Auth } from '../../../core/services/auth';
import { RouterLink } from "@angular/router";

@Component({
  selector: 'app-main-view',
  imports: [RouterLink],
  templateUrl: './main-view.html',
  styleUrl: './main-view.scss',
})
export class MainView {

  authService = inject(Auth);


  logout() : void {
    this.authService.logout();
  }
}
