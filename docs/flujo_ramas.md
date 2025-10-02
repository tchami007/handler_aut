# Flujo de trabajo con ramas Git

## 1. Rama principal (`main`)
- Código estable y listo para producción.
- Solo se actualiza mediante merge de ramas `develop` o release.

## 2. Rama de desarrollo (`develop`)
- Base para integración de nuevas funcionalidades y correcciones.
- Se actualiza mediante merge de ramas `feature` y `hotfix`.
- Cuando se alcanza una versión estable, se fusiona en `main`.

## 3. Ramas de funcionalidad (`feature/*`)
- Se crean a partir de `develop` para nuevas características.
- Ejemplo: `feature/estructura-inicial`, `feature/api-debito`.
- Al finalizar, se fusionan en `develop` y se eliminan.

## 4. Ramas de corrección (`hotfix/*`)
- Se crean a partir de `main` para solucionar errores críticos en producción.
- Al finalizar, se fusionan en `main` y en `develop`.

## 5. Ramas de release (`release/*`)
- Se crean a partir de `develop` para preparar una nueva versión.
- Permiten ajustes menores y pruebas finales antes de pasar a producción.
- Al finalizar, se fusionan en `main` y en `develop`.

---

### Ejemplo de ciclo de trabajo

1. **Crear rama de funcionalidad:**
   ```
   git checkout develop
   git checkout -b feature/nueva-funcionalidad
   ```
   *Cambia a la rama `develop` y crea una nueva rama `feature/nueva-funcionalidad` para trabajar en una nueva funcionalidad.*

2. **Desarrollar y hacer commits en la rama `feature`.**
   *Realiza los cambios y guarda el progreso en la rama de funcionalidad.*

3. **Al terminar, fusionar en `develop`:**
   ```
   git checkout develop
   git merge feature/nueva-funcionalidad
   git branch -d feature/nueva-funcionalidad
   ```
   *Cambia a la rama `develop`, fusiona los cambios de la rama de funcionalidad y elimina la rama local de la funcionalidad.*

4. **Cuando la versión está lista, crear rama de release:**
   ```
   git checkout develop
   git checkout -b release/v1.0.0
   ```
   *Cambia a la rama `develop` y crea una nueva rama `release` para preparar la versión final.*

5. **Realizar ajustes, luego fusionar en `main` y en `develop`:**
   ```
   git checkout main
   git merge release/v1.0.0
   git checkout develop
   git merge release/v1.0.0
   git branch -d release/v1.0.0
   ```
   *Fusiona la rama de release en `main` (producción) y en `develop` (desarrollo), luego elimina la rama de release.*

---

Este flujo facilita el trabajo colaborativo, la integración continua y el control de versiones en proyectos de software.
