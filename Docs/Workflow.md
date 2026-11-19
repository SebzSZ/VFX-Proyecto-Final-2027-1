# Flujo de Trabajo

Flujo diario de Git + Unity

---

## Ramas

```text
main
 ↑
develop
 ↑
rama de trabajo
```

- `main`: versión estable y entregable.
- `develop`: integración actual.
- Cada tarea utiliza una rama independiente.

No trabajar directamente sobre `main` o `develop`.

---

## Empezar una tarea

```bash
git checkout develop
git pull --ff-only origin develop
git checkout -b feature/player-movement
```

Usar el prefijo correspondiente:

```text
feature/
art/
vfx/
fix/
chore/
```

---

## Durante el trabajo

Hacer commits pequeños y claros.

Antes de cada commit:

```text
[ ] Guardar escenas y prefabs.
[ ] Salir de Play Mode.
[ ] Revisar Console.
[ ] Ejecutar git status.
[ ] Confirmar que los archivos modificados son esperados.
[ ] Comprobar los .meta.
```

Después:

```bash
git add .
git commit -m "feat: implement player movement"
git push
```

---

## Pull Request

Toda rama de trabajo se integra mediante:

```text
rama
 ↓
Pull Request
 ↓
develop
```

El PR debe indicar brevemente:

```text
Qué cambió
Cómo se probó
Qué debe revisar el otro integrante
Problemas conocidos
```

Agregar captura o video cuando el cambio sea visual.

---

## Actualizar una rama

Si `develop` cambió:

```bash
git fetch origin
git merge origin/develop
```

Después:

```text
Resolver conflictos
↓
Abrir Unity
↓
Revisar Console
↓
Probar
↓
Commit
↓
Push
```
