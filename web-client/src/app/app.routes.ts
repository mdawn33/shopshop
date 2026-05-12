import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '', redirectTo: 'catalog', pathMatch: 'full'
    },
    {
        path: 'auth', 
        loadChildren: () => import('./features/auth/auth.routes').then(m => m.default)
    },
    {
        path: 'catalog', 
        loadChildren: () => import('./features/catalog/catalog.routes').then(m => m.default)
    },
    {
        path: 'product', 
        loadChildren: () => import('./features/product/product.routes').then(m => m.default)
    },
    {
        path: 'cart', 
        loadChildren: () => import('./features/cart/cart.routes').then(m => m.default)
    },
    {
        path: 'checkout', 
        loadChildren: () => import('./features/checkout/checkout.routes').then(m => m.default)
    },
    {
        path: 'orders', 
        loadChildren: () => import('./features/orders/orders.routes').then(m => m.default)
    },
    {
        path: '**',
        loadComponent: () => import('./features/not-found/not-found').then(m => m.NotFound)
    }
];
