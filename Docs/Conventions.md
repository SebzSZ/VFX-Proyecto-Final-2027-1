# Convenciones y Nomenclatura

Referencia rápida para nombres dentro del proyecto.

Todo nombre técnico debe escribirse en inglés.

Evitar espacios, acentos, nombres ambiguos y sufijos como:

```text
final
new
v2
test123
```

---

## C#

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clase | `PascalCase` | `PlayerController` |
| Método | `PascalCase` | `ReceiveDamage()` |
| Propiedad | `PascalCase` | `CurrentHealth` |
| Campo privado | `_camelCase` | `_currentHealth` |
| Variable/parámetro | `camelCase` | `damageAmount` |
| Interfaz | `IPascalCase` | `IDamageable` |

### Namespaces

```csharp
FinalVFX.Core
FinalVFX.Player
FinalVFX.Combat
FinalVFX.Boss
FinalVFX.UI
FinalVFX.Audio
FinalVFX.VFX
```

---

## Assets

| Tipo | Formato | Ejemplo |
|---|---|---|
| Escena | `SCN_` | `SCN_Arena` |
| Prefab | `PF_` | `PF_Player` |
| Material | `MAT_` | `MAT_GuardianArmor` |
| Textura | `T_` | `T_GuardianArmor_Normal` |
| Animación | `AN_` | `AN_Player_Dodge` |
| Animator | `AC_` | `AC_Player` |
| ScriptableObject | `SO_` | `SO_GuardianBalance` |
| Shader Graph | `SG_` | `SG_BossDissolve` |
| Shader | `SH_` | `SH_CustomLighting` |
| VFX | `VFX_` | `VFX_FireProjectile` |
| SFX | `SFX_` | `SFX_SwordImpact` |
| Input Actions | `INPUT_` | `INPUT_PlayerControls` |
| UI | `UI_` | `UI_PlayerHUD` |

---

## Texturas

Formato:

```text
T_<Asset>_<Map>
```

Ejemplo:

```text
T_PlayerArmor_BaseColor
T_PlayerArmor_Normal
```

---

## GameObjects

Usar nombres descriptivos:

```text
VisualRoot
GameplayRoot
WeaponSocket
PlayerSpawn
CameraTarget
AttackOrigin
ArenaBounds
```

Evitar:

```text
GameObject
Empty
Cube (3)
New Game Object
```

---

## Ramas

```text
main
develop

feature/<name>
art/<name>
vfx/<name>
fix/<name>
chore/<name>
hotfix/<name>
```

Ejemplos:

```text
feature/player-movement
art/guardian-model
vfx/fire-projectile
fix/player-collider
```

---

## Commits

Formato:

```text
<type>: <description>
```

Tipos:

```text
feat:
fix:
art:
refactor:
docs:
chore:
```

Ejemplos:

```text
feat: add player stamina component
art: add guardian placeholder mesh
vfx: add sword impact particles
fix: prevent duplicate damage
```

Evitar commits como:

```text
changes
update
final
stuff
it works
```

---

## Regla general

Un nombre debe indicar qué es y qué hace sin tener que abrir el archivo.

Preferir:

```text
VFX_FireProjectileImpact
```

sobre:

```text
VFX_Effect02
```