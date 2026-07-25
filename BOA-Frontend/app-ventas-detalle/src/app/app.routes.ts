import { Routes } from '@angular/router';
import { DashboardLayoutComponent } from './shared/layouts/dashboardLayout/dashboardLayout.component';
import { authGuard } from './shared/guard/auth-guard.service';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./modules/landing/landing/landing.component').then(m => m.LandingComponent),
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: () =>
      import('./modules/auth/auth.route').then(m => m.auth_routes),
  },
  {
    path: 'dashboard',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      {
        path: 'roles',
        loadChildren: () =>
          import('./modules/rol/rol.route').then(m => m.rol_routes),
        data: { icon: 'pi pi-users', title: 'Roles', description: 'Gestion de Roles', permission: 'roles' },
      },
      {
        path: 'roles-permisos',
        loadChildren: () =>
          import('./modules/roles-permisos/roles-permisos.route').then(m => m.roles_permisos_routes),
        data: { icon: 'pi pi-users', title: 'Asignacion de Permisos', description: 'Gestion de Permisos a Roles', permission: 'permisos' },
      },
      {
        path: 'user',
        loadChildren: () =>
          import('./modules/usuario/usuario.route').then(m => m.usuario_routes),
        data: { icon: 'pi pi-users', title: 'Usuario', description: 'Gestion de Usuarios', permission: 'usuarios' },
      },
      {
        path: 'aeropuerto',
        loadChildren: () =>
          import('./modules/aeropuerto/aeropuerto.route').then(m => m.aeropuerto_routes),
        data: { icon: 'pi pi-map-marker', title: 'Aeropuerto', description: 'Gestion de Aeropuertos', permission: 'aeropuertos' },
      },
      {
        path: 'aeronave',
        loadChildren: () =>
          import('./modules/aeronave/aeronave.route').then(m => m.aeronave_routes),
        data: { icon: 'pi pi-send', title: 'Aeronave', description: 'Gestion de Aeronaves', permission: 'aeronaves' },
      },
      {
        path: 'empleado',
        loadChildren: () =>
          import('./modules/empleado/empleado.route').then(m => m.empleado_routes),
        data: { icon: 'pi pi-id-card', title: 'Empleado', description: 'Gestion de Empleados', permission: 'empleados' },
      },
      {
        path: 'ruta',
        loadChildren: () =>
          import('./modules/ruta/ruta.route').then(m => m.ruta_routes),
        data: { icon: 'pi pi-directions', title: 'Ruta', description: 'Gestion de Rutas', permission: 'rutas' },
      },
      {
        path: 'tramo',
        loadChildren: () =>
          import('./modules/tramo/tramo.route').then(m => m.tramo_routes),
        data: { icon: 'pi pi-map', title: 'Tramo', description: 'Gestion de Tramos', permission: 'tramos' },
      },
      {
        path: 'programacion-vuelo',
        loadChildren: () =>
          import('./modules/programacion-vuelo/programacion-vuelo.route').then(m => m.programacion_vuelo_routes),
        data: { icon: 'pi pi-calendar', title: 'Programacion de Vuelos', description: 'Gestion de Programacion de Vuelos', permission: 'programacion_vuelos' },
      },
      {
        path: 'tipo-clase',
        loadChildren: () =>
          import('./modules/tipo-clase/tipo-clase.route').then(m => m.tipo_clase_routes),
        data: { icon: 'pi pi-star', title: 'Tipo de Clase', description: 'Gestion de Tipos de Clase', permission: 'tipo_clases' },
      },
      {
        path: 'ventas',
        loadChildren: () =>
          import('./modules/venta/venta.route').then(m => m.venta_routes),
        data: { icon: 'pi pi-shopping-cart', title: 'Ventas', description: 'Gestionar Ventas', permission: 'ventas' },
      },
      {
        path: 'cliente',
        loadChildren: () =>
          import('./modules/cliente/cliente.route').then(m => m.cliente_routes),
        data: { icon: 'pi pi-ticket', title: 'Buscar Vuelos', description: 'Buscar Vuelos Disponibles', permission: 'compras' },
      },
      {
        path: 'ingresos',
        loadChildren: () =>
          import('./modules/ingreso/ingreso.route').then(m => m.ingreso_routes),
        data: { icon: 'pi pi-arrow-up', title: 'Ingresos', description: 'Flujo de Ingresos', permission: 'ingresos' },
      },
      {
        path: 'egresos',
        loadChildren: () =>
          import('./modules/egreso/egreso.route').then(m => m.egreso_routes),
        data: { icon: 'pi pi-arrow-down', title: 'Egresos', description: 'Flujo de Egresos', permission: 'egresos' },
      },
      {
        path: 'reportes',
        loadChildren: () =>
          import('./modules/reportes/reportes.route').then(m => m.reportes_routes),
        data: { icon: 'pi pi-chart-bar', title: 'Reportes', description: 'Informes y Estadísticas', permission: 'reportes' },
      },
      {
        path: 'cliente-admin',
        loadChildren: () =>
          import('./modules/cliente-admin/cliente-admin.route').then(m => m.cliente_admin_routes),
        data: { icon: 'pi pi-users', title: 'Clientes', description: 'Base de Pasajeros', permission: 'clientes' },
      },
      {
        path: 'tripulacion',
        loadChildren: () =>
          import('./modules/tripulacion/tripulacion.route').then(m => m.tripulacion_routes),
        data: { icon: 'pi pi-users', title: 'Tripulación', description: 'Asignación de Crew', permission: 'tripulaciones' },
      },
      {
        path: 'salidas',
        loadChildren: () =>
          import('./modules/salida/salida.route').then(m => m.salida_routes),
        data: { icon: 'pi pi-sign-out', title: 'Salidas', description: 'Control de Abordaje', permission: 'salidas' },
      },
      {
        path: 'consultar-vuelo',
        loadChildren: () =>
          import('./modules/consultar-vuelo/consultar-vuelo.route').then(m => m.consultar_vuelo_routes),
        data: { icon: 'pi pi-search', title: 'Consultar Vuelos', description: 'Ocupación y Pasajeros', permission: 'asientos' },
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
    pathMatch: 'full',
  },
];