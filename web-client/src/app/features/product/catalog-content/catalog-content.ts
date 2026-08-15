import { Component, inject, signal } from '@angular/core';
import { ProductRail } from '../../product/product-rail/product-rail';
import { CatalogState } from '../../../core/services/catalog-state';
import { CATALOG_SECTIONS, PROMO_BANNERS } from '../../../shared/data/sample-data';
import { CatalogSection } from '../../../core/models/catalog-section';
import { PromoCarousel } from '../promo-carousel/promo-carousel';

@Component({
  selector: 'app-catalog-content',
  imports: [ProductRail, PromoCarousel],
  templateUrl: './catalog-content.html',
  styleUrl: './catalog-content.scss',
})
export class CatalogContent {

  protected readonly stateService = inject(CatalogState);

  
  protected readonly catalogSections = signal<CatalogSection[]>(CATALOG_SECTIONS);

  protected readonly promoBanners = PROMO_BANNERS;

}
