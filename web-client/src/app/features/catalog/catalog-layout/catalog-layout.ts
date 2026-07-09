import { Component, signal, computed } from '@angular/core';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { ProductRail } from '../../product/product-rail/product-rail';
import { PromoCarousel } from '../../../shared/layout/promo-carousel/promo-carousel';
import { Footer } from '../../../shared/layout/footer/footer';
import { CATALOG_SECTIONS, CATEGORIES, NAV_LINKS, PROMO_BANNERS } from '../../../shared/data/sample-data';
import { RouterOutlet } from "../../../../../node_modules/@angular/router/types/_router_module-chunk";
import { Product } from '../../../core/models/product';

@Component({
  selector: 'app-catalog-layout',
  imports: [Header, Sidebar, ProductRail, PromoCarousel, Footer, RouterOutlet],
  templateUrl: './catalog-layout.html',
  styleUrl: './catalog-layout.scss',
})
export class CatalogLayout {

  // Signals fire the update when the reference changes
  // Signal.update() always produces a new reference, so it triggers change detection
  
  readonly deliveryLocation = signal('Bogotá, Cundinamarca');
  readonly activeCategory = signal('todas');
  readonly cart = signal<Record<string, number>>({});
  readonly wishlist = signal<ReadonlySet<string>>(new Set());
 
  readonly cartCount = computed(() =>
    Object.values(this.cart()).reduce((sum, qty) => sum + qty, 0)
  );
 
  readonly categories = CATEGORIES;
  readonly promoBanners = PROMO_BANNERS;
  readonly sections = CATALOG_SECTIONS;
  readonly navLinks = NAV_LINKS;


  addToCart(product: Product): void {
    this.cart.update((current) => ({
      ...current,
      [product.id]: (current[product.id] ?? 0) + 1,
    }));
  }

  onSearch(query: string): void {
    console.log("Search: ", query);
  }

  toggleWishlist(product: Product): void {
    this.wishlist.update((current) => {
      const next = new Set(current); // create a brand new Set copied from the current one
      next.has(product.id) ? next.delete(product.id) : next.add(product.id);
      return next; // replace the signal's value with the new Set
    });
  }
 
  onNewsletterSubscribe(email: string): void {
    console.log('Suscribir:', email);
  }

  selectCategory(id: string) : void {
    this.activeCategory.set(id);
  }
}
