import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PromoCarousel } from '../../../shared/layout/promo-carousel/promo-carousel';
import { ProductRail } from '../../product/product-rail/product-rail';
import { CATALOG_SECTIONS, PROMO_BANNERS } from '../../../shared/data/sample-data';
import { CatalogState } from '../../../core/services/catalog-state';

@Component({
  selector: 'app-catalog-content',
  standalone: true,
  imports: [PromoCarousel, ProductRail],
  templateUrl: './catalog-content.html',
  styleUrl: './catalog-content.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CatalogContent {
  protected readonly state = inject(CatalogState);
  
  readonly promoBanners = PROMO_BANNERS;
  readonly sections = CATALOG_SECTIONS;

}
