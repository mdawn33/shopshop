import { Component, inject } from '@angular/core';
import { Auth } from '../../../core/services/auth';
import { RouterLink } from "@angular/router";
import { CATALOG_SECTIONS } from '../../../shared/data/sample-data';
import { ProductRail } from '../../product/product-rail/product-rail';
import { CatalogState } from '../../../core/services/catalog-state';
import { Product } from '../../../core/models/product';

@Component({
  selector: 'app-main-view',
  imports: [RouterLink, ProductRail],
  templateUrl: './main-view.html',
  styleUrl: './main-view.scss',
})
export class MainView {

  authService = inject(Auth);
  stateService = inject(CatalogState);


  protected readonly catalogSections = CATALOG_SECTIONS;


  logout() : void {
    this.authService.logout();
  }
}
