import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';

/**
 * Lee de forma defensiva los permisos (nombre_Permiso) del usuario desde
 * sessionStorage['roles']. La estructura esperada es:
 *   [{ nombre_Rol, permisos: [{ nombre_Permiso }, ...] }, ...]
 * Si 'roles' está ausente, vacío o el JSON está corrupto, devuelve [] (sin permisos)
 * en lugar de lanzar una excepción.
 */
function getPermisos(): string[] {
  try {
    const raw = sessionStorage.getItem('roles');
    if (!raw) return [];
    const roles = JSON.parse(raw);
    if (!Array.isArray(roles)) return [];
    return roles
      .flatMap((rol: any) => (Array.isArray(rol?.permisos) ? rol.permisos : []))
      .map((permiso: any) => permiso?.nombre_Permiso)
      .filter((nombre: any): nombre is string => typeof nombre === 'string');
  } catch {
    return [];
  }
}

function tienePermiso(permiso: string): boolean {
  return getPermisos().includes(permiso);
}

/**
 * Guard de acceso reutilizando el `data.permission` que ya declaran las rutas.
 *  - Sin token           -> /auth/login
 *  - Con token, con permiso (o ruta sin permiso declarado) -> acceso
 *  - Con token, sin permiso -> destino seguro:
 *        Cliente (permiso 'compras') -> /dashboard/cliente/buscar-vuelos
 *        resto                       -> /dashboard
 */
export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const router = inject(Router);

  const token = sessionStorage.getItem('token');
  if (!token) {
    return router.createUrlTree(['/auth/login']);
  }

  const permisoRequerido: string | undefined = route.data?.['permission'];

  // Ruta sin permiso declarado (p. ej. el layout): basta con estar autenticado.
  if (!permisoRequerido) {
    return true;
  }

  if (tienePermiso(permisoRequerido)) {
    return true;
  }

  // Autenticado pero sin permiso -> a una pantalla que el usuario sí pueda ver.
  const destino = tienePermiso('compras')
    ? ['/dashboard/cliente/buscar-vuelos']
    : ['/dashboard'];
  return router.createUrlTree(destino);
};
