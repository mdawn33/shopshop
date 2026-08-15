import { Routes } from '@angular/router';
import { authGuard, claimGuard } from './core/guards/auth';

export const routes: Routes = [
    {
        path: '', redirectTo: 'product', pathMatch: 'full'
    },
    {
        path: 'auth', 
        loadChildren: () => import('./features/auth/auth.routes').then(m => m.default)
    },
    // { 
    //     path: 'admin', 
    //     component: ManagementComponent, 
    //     canActivate: [claimGuard('role', 'Admin')] // Requires standard "Admin" token profile claim
    // },    
    {
        path: 'product',
        loadChildren: () => import('./features/product/product.routes').then(m => m.default),
        canActivate: [authGuard]
    },
    {
        path: 'cart', 
        loadChildren: () => import('./features/cart/cart.routes').then(m => m.default),
        canActivate: [authGuard]
    },
    {
        path: 'checkout', 
        loadChildren: () => import('./features/checkout/checkout.routes').then(m => m.default),
        canActivate: [claimGuard('role', 'customer')]
    },
    {
        path: 'orders', 
        loadChildren: () => import('./features/orders/orders.routes').then(m => m.default),
        canActivate: [claimGuard('role', 'customer')]

    },
    // {
    //     path: 'unauthorized',
    //     component: UnauthorizedComponent
    //  },
    {
        path: '**',
        loadComponent: () => import('./features/not-found/not-found').then(m => m.NotFound)
    }
];
