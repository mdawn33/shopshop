import { CatalogSection } from "../../core/models/catalog-section";
import { Category } from "../../core/models/category";
import { NavLink } from "../../core/models/navlink";
import { Product } from "../../core/models/product";
import { PromoBanner } from "../../core/models/promo-banner";


// ---------------------------------------------------------------------------
// Navigation links
// ---------------------------------------------------------------------------
export const NAV_LINKS: NavLink[] = [
  { label: 'Recomendados para ti', href: '#recomendados' },
  { label: 'Más vendidos',         href: '#mas-vendidos' },
  { label: 'Ofertas del día',      href: '#ofertas' },
  { label: 'Nuevos ingresos',      href: '#nuevos' },
  { label: 'Envío gratis',         href: '#envio-gratis' },
];

// ---------------------------------------------------------------------------
// Categories
// ---------------------------------------------------------------------------
export const CATEGORIES: Category[] = [
  { id: 'todas',        label: 'Todas las categorías' },
  { id: 'electronica',  label: 'Electrónica y tecnología' },
  { id: 'celulares',    label: 'Celulares y accesorios' },
  { id: 'computo',      label: 'Cómputo y tablets' },
  { id: 'hogar',        label: 'Hogar y electrodomésticos' },
  { id: 'moda',         label: 'Moda y accesorios' },
  { id: 'deportes',     label: 'Deportes y actividad física' },
  { id: 'belleza',      label: 'Belleza y cuidado personal' },
  { id: 'juguetes',     label: 'Juguetes y juegos' },
  { id: 'mascotas',     label: 'Mascotas' },
  { id: 'herramientas', label: 'Herramientas y construcción' },
  { id: 'alimentos',    label: 'Alimentos y bebidas' },
];

// ---------------------------------------------------------------------------
// Promo banners
// ---------------------------------------------------------------------------
export const PROMO_BANNERS: PromoBanner[] = [
  { id: 'b1', imageUrl: 'https://picsum.photos/seed/promo-tech-1/640/280',    alt: 'Hasta 40% de descuento en electrónica esta semana' },
  { id: 'b2', imageUrl: 'https://picsum.photos/seed/promo-fashion-1/640/280', alt: 'Nueva colección de moda — envío gratis por $150.000' },
  { id: 'b3', imageUrl: 'https://picsum.photos/seed/promo-home-1/640/280',    alt: 'Renueva tu hogar: ofertas en electrodomésticos' },
  { id: 'b4', imageUrl: 'https://picsum.photos/seed/promo-sports-1/640/280',  alt: 'Equipate para el deporte — descuentos de temporada' },
];

// ---------------------------------------------------------------------------
// Products
// ---------------------------------------------------------------------------
function product(
  id: string, name: string, brand: string, sku: string, variant: string,
  rating: number, reviewCount: number, price: number, originalPrice: number,
  stock: number, fastShipping: boolean
): Product {
  return {
    id, name, brand, sku, variant, rating, reviewCount,
    price, originalPrice, stock, fastShipping,
    imageUrl: `https://picsum.photos/seed/product-${id}/300/300`,
  };
}

export const CATALOG_SECTIONS: CatalogSection[] = [
  {
    id: 'mas-vendidos',
    title: 'Más vendidos con envío rápido',
    link: '#',
    products: [
      product('1',  'Audífonos Inalámbricos Pro',       'Sony',         'SNY-WH1000XM5',  'Negro',          4.8, 3420, 689000,  980000,  100, true),
      product('2',  'Smartwatch Fitness Band 7',        'Samsung',      'SM-R220NZKALAT', 'Grafito 44mm',   4.6, 1870, 459000,  599000,  58,  true),
      product('3',  'Licuadora de Alta Potencia 1000W', 'Oster',        'OST-BLSTHB1',    'Acero/Negro',    4.5,  892, 189000,  249000,  100, true),
      product('4',  'Zapatillas Running Air Zoom',      'Nike',         'NIK-AZ-008-BLK', 'Talla 42 Negro', 4.7, 2105, 379000,  499000,  35,  true),
      product('5',  'Silla Ergonómica Mesh Pro',        'Marca Propia', 'MP-SILLA-001',   'Negro/Gris',     4.4,  540, 529000,  749000,  20,  true),
      product('6',  'Cafetera Espresso 15 Bar',         'Nespresso',    'NSP-ES15-TIT',   'Titanio',        4.9, 1204, 449000,  599000,  100, true),
      product('7',  'Teclado Mecánico RGB TKL',         'Logitech',     'LGT-G413-SE',    'Blanco',         4.6,  678, 289000,  359000,  100, true),
      product('8',  'Mochila Antirrobo 25L',            'Samsonite',    'SAM-BT25-GRY',   'Gris Oscuro',    4.5,  420, 229000,  299000,  3,   true),
    ],
  },
  {
    id: 'ofertas',
    title: 'Ofertas del día',
    link: '#',
    products: [
      product('9',  'Smart TV 50" 4K UHD',              'LG',           'LG-50UP7500',    '50 pulgadas',    4.7, 2890, 1299000, 2199000, 15,  false),
      product('10', 'Tablet 10.4" 64GB',                'Samsung',      'SM-T505NZAACOO', 'Plata 64GB',     4.5, 1130,  899000, 1299000, 48,  false),
      product('11', 'Robot Aspirador con Mapeo',        'Xiaomi',       'XM-ROBOROCK-S5', 'Blanco',         4.6,  760,  679000, 1199000, 22,  false),
      product('12', 'Parlante Portátil Bluetooth 30W',  'JBL',          'JBL-CHARGE5-BK', 'Negro',          4.8, 3310,  459000,  699000, 100, false),
      product('13', 'Freidora de Aire 5.5L Digital',    'Philips',      'PHI-HD9270-90',  'Negro',          4.4,  605,  349000,  549000, 37,  false),
      product('14', 'Monitor Gamer 27" 165Hz IPS',      'MSI',          'MSI-G274QPX',    '27" QHD',        4.7,  489, 1099000, 1599000, 11,  false),
    ],
  },
  {
    id: 'nuevos',
    title: 'Nuevos ingresos',
    link: '#',
    products: [
      product('15', 'Cámara Instantánea Mini 12',       'Fujifilm',        'FUJ-INSTAX12P',  'Lila Pastel',   4.3,  87, 359000,  359000, 50,  true),
      product('16', 'Perfume Aqua Di Gio 100ml',        'Giorgio Armani',  'GA-ADG-100M',    'Hombre 100ml',  4.9, 412, 499000,  599000, 30,  true),
      product('17', 'Set Skincare Vitamina C',          'The Ordinary',    'TO-VITC-SET3',   'Kit x3 piezas', 4.6, 158, 189000,  229000, 100, true),
      product('18', 'Patineta Eléctrica 350W',          'Xiaomi',          'XM-SCOOTER-4',   'Negra 25km/h',  4.5, 203, 1199000, 1399000, 8,  false),
      product('19', 'Auriculares Gamer 7.1 Surround',   'HyperX',          'HX-HSCC2-BK',   'Negro/Rojo',    4.4,  95, 259000,  259000, 63,  true),
      product('20', 'Crema Hidratante SPF 50 50ml',     'Neutrogena',      'NEU-HYDR-SPF50', '50ml FPS 50',   4.7, 330,  69000,   89000, 100, true),
    ],
  },
];