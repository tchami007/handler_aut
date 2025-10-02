# Instrucciones para el control de versiones (Git)

A continuación se detallan los comandos ejecutados para configurar el control de versiones en el proyecto, junto con su explicación:

---

## 1. Inicializar el repositorio Git
```bash
git init
```
*Inicializa un nuevo repositorio Git en la carpeta raíz del proyecto.*

## 2. Crear archivo .gitignore para proyectos .NET
*Se creó el archivo `.gitignore` con exclusiones recomendadas para proyectos .NET, Entity Framework y archivos temporales.*

## 3. Agregar todos los archivos al repositorio
```bash
git add .
```
*Agrega todos los archivos y carpetas actuales al área de preparación de Git.*

## 4. Realizar el primer commit
```bash
git commit -m "Inicialización del proyecto y configuración base"
```
*Guarda el estado inicial del proyecto en el repositorio con un mensaje descriptivo.*

## 5. Renombrar la rama principal a 'main'
```bash
git branch -m master main
```
*Renombra la rama principal de 'master' a 'main' para seguir las convenciones actuales.*

## 6. Crear rama de desarrollo
```bash
git checkout -b develop
```
*Crea y cambia a la rama 'develop', utilizada para desarrollo continuo.*

## 7. Crear rama de funcionalidad
```bash
git checkout -b feature/estructura-inicial
```
*Crea y cambia a una rama de funcionalidad para trabajar en la estructura inicial del proyecto.*

---

Estas instrucciones establecen una base sólida para el control de versiones y el trabajo colaborativo en el proyecto.
