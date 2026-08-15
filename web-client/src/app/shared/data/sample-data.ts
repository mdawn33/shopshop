import { CatalogSection } from "../../core/models/catalog-section";
import { Category } from "../../core/models/category";
import { Navlink } from "../../core/models/navlink";
import { Product } from "../../core/models/product";
import { PromoBanner } from "../../core/models/promo-banner";


// ---------------------------------------------------------------------------
// Navigation links
// ---------------------------------------------------------------------------
export const NAV_LINKS: Navlink[] = [
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
  { id: '1', name: 'todas',        label: 'Todas las categorías' },
  { id: '2', name: 'electronica',  label: 'Electrónica y tecnología' },
  { id: '3', name: 'celulares',    label: 'Celulares y accesorios' },
  { id: '4', name: 'computo',      label: 'Cómputo y tablets' },
  { id: '5', name: 'hogar',        label: 'Hogar y electrodomésticos' },
  { id: '6', name: 'moda',         label: 'Moda y accesorios' },
  { id: '7', name: 'deportes',     label: 'Deportes y actividad física' },
  { id: '8', name: 'belleza',      label: 'Belleza y cuidado personal' },
  { id: '9', name: 'juguetes',     label: 'Juguetes y juegos' },
  { id: '10', name: 'mascotas',     label: 'Mascotas' },
  { id: '11', name: 'herramientas', label: 'Herramientas y construcción' },
  { id: '12', name: 'alimentos',    label: 'Alimentos y bebidas' },
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
  id: string, name: string, description: string, brand_author: string, sku: string, categoryId: string, variant: string,
  rating: number, reviewCount: number, price: number, originalPrice: number,
  stock: number, fastShipping: boolean
): Product {
  return {
    id, name, description, brand_author, sku, categoryId, variant, rating, reviewCount,
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
      product('1',  'Audífonos Inalámbricos Pro',      'Audífonos Inalámbricos Pro',       'Sony',         'SNY-WH1000XM5', '2',  'Negro',          4.8, 3420, 689000,  980000,  100, true),
      product('2',  'Smartwatch Fitness Band 7',       'Smartwatch Fitness Band 7',        'Samsung',      'SM-R220NZKALAT','2', 'Grafito 44mm',   4.6, 1870, 459000,  599000,  58,  true),
      product('3',  'Licuadora de Alta Potencia 1000W','Licuadora de Alta Potencia 1000W', 'Oster',        'OST-BLSTHB1',  ' 5',  'Acero/Negro',    4.5,  892, 189000,  249000,  100, true),
      product('4',  'Zapatillas Running Air Zoom',     'Zapatillas Running Air Zoom',      'Nike',         'NIK-AZ-008-BLK','7', 'Talla 42 Negro', 4.7, 2105, 379000,  499000,  35,  true),
      product('5',  'Silla Ergonómica Mesh Pro',       'Silla Ergonómica Mesh Pro',        'Marca Propia', 'MP-SILLA-001',  '4',  'Negro/Gris',     4.4,  540, 529000,  749000,  20,  true),
      product('6',  'Cafetera Espresso 15 Bar',        'Cafetera Espresso 15 Bar',         'Nespresso',    'NSP-ES15-TIT',  '5',  'Titanio',        4.9, 1204, 449000,  599000,  100, true),
      product('7',  'Teclado Mecánico RGB TKL',        'Teclado Mecánico RGB TKL',         'Logitech',     'LGT-G413-SE',   '2',  'Blanco',         4.6,  678, 289000,  359000,  100, true),
      product('8',  'Mochila Antirrobo 25L',           'Mochila Antirrobo 25L',            'Samsonite',    'SAM-BT25-GRY',  '4',  'Gris Oscuro',    4.5,  420, 229000,  299000,  3,   true),
    ],
  },
  {
    id: 'ofertas',
    title: 'Ofertas del día',
    link: '#',
    products: [
      product('9' ,  'Smart TV 50" 4K UHD',            'Smart TV 50" 4K UHD',              'LG',           'LG-50UP7500',  '2',  '50 pulgadas',    4.7, 2890, 1299000, 2199000, 15,  false),
      product('10', 'Tablet 10.4" 64GB',              'Tablet 10.4" 64GB',                'Samsung',      'SM-T505NZAACOO','2', 'Plata 64GB',     4.5, 1130,  899000, 1299000, 48,  false),
      product('11', 'Robot Aspirador con Mapeo',      'Robot Aspirador con Mapeo',        'Xiaomi',       'XM-ROBOROCK-S5','5', 'Blanco',         4.6,  760,  679000, 1199000, 22,  false),
      product('12', 'Parlante Portátil Bluetooth 30W','Parlante Portátil Bluetooth 30W',  'JBL',          'JBL-CHARGE5-BK','2', 'Negro',          4.8, 3310,  459000,  699000, 100, false),
      product('13', 'Freidora de Aire 5.5L Digital',  'Freidora de Aire 5.5L Digital',    'Philips',      'PHI-HD9270-90', '5', 'Negro',          4.4,  605,  349000,  549000, 37,  false),
      product('14', 'Monitor Gamer 27" 165Hz IPS',    'Monitor Gamer 27" 165Hz IPS',      'MSI',          'MSI-G274QPX',   '2', '27" QHD',        4.7,  489, 1099000, 1599000, 11,  false),
    ],
  },
  {
    id: 'nuevos',
    title: 'Nuevos ingresos',
    link: '#',
    products: [
      product('15', 'Cámara Instantánea Mini 12',    'Cámara Instantánea Mini 12',       'Fujifilm',        'FUJ-INSTAX12P', '2', 'Lila Pastel',   4.3,  87, 359000,  359000, 50,  true),
      product('16', 'Perfume Aqua Di Gio 100ml',     'Perfume Aqua Di Gio 100ml',        'Giorgio Armani',  'GA-ADG-100M',   '8', 'Hombre 100ml',  4.9, 412, 499000,  599000, 30,  true),
      product('17', 'Set Skincare Vitamina C',       'Set Skincare Vitamina C',          'The Ordinary',    'TO-VITC-SET3',  '12', 'Kit x3 piezas', 4.6, 158, 189000,  229000, 100, true),
      product('18', 'Patineta Eléctrica 350W',       'Patineta Eléctrica 350W',          'Xiaomi',          'XM-SCOOTER-4',  '2', 'Negra 25km/h',  4.5, 203, 1199000, 1399000, 8,  false),
      product('19', 'Auriculares Gamer 7.1 Surround','Auriculares Gamer 7.1 Surround',   'HyperX',          'HX-HSCC2-BK',   '4','Negro/Rojo',    4.4,  95, 259000,  259000, 63,  true),
      product('20', 'Crema Hidratante SPF 50 50ml',  'Crema Hidratante SPF 50 50ml',     'Neutrogena',      'NEU-HYDR-SPF50','8', '50ml FPS 50',   4.7, 330,  69000,   89000, 100, true),
    ],
  },
];

export function getProductById(productId: string): Product | undefined {
  return CATALOG_SECTIONS.flatMap(s => s.products).find(p => p.id === productId);
}