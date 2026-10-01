# Pruebas de vida y consumibles

Abre `Assets/Scenes/Dev/SCN_Dev_Combat.unity` y pulsa Play.

- La zona roja está siempre activa. Al entrar recibes 25 de daño; si permaneces dentro, se repite una vez por segundo.
- El dummy azul también causa daño por contacto, con la misma cadencia. Ya no tiene vida ni barra de vida.
- Aléjate de ambas fuentes para dejar de recibir daño.
- Pulsa Q o cruceta abajo, estando en el suelo, para curarte 30 puntos. El número blanco abajo a la izquierda indica las cargas restantes (inicialmente 3).
- No se gastan cargas con vida llena, muerto, sin cargas o durante el cooldown de 0.75 s.
- Al morir, el encuentro se recarga después de 2 segundos: vida, estamina y cargas se restauran.

Se eliminaron el panel de depuración y los atajos F1/F6/F7. Se conservan las barras de vida y estamina del jugador.

## Referencias y configuración

| Objeto | Componente | Configuración |
|---|---|---|
| TestingRoot/DamageZone | ContactDamage | Target Layers: Player; Damage: 25; Interval: 1 s. |
| CombatDummy/ContactVolume | ContactDamage | Igual que la zona; trigger alrededor del collider sólido del dummy. |
| UIRoot/Canvas/UI_ConsumableCounter | ConsumableCounter | Consumable: HealingConsumable de PF_Player. |
| GameplayRoot/EncounterRoot | EncounterRestart | Player Health: HealthComponent de PF_Player. |

ContactDamage usa un trigger y un Rigidbody kinematic sin gravedad. El intervalo se comparte entre los colliders de un mismo objetivo para evitar impactos duplicados. Las dos fuentes tienen temporizadores independientes: si te superpones a ambas, ambas pueden dañarte. Una protección de invulnerabilidad puede rechazar un impacto; el siguiente intento respeta la cadencia de la fuente.

El contador usa Text de Unity UI dentro del Canvas, con anclas en la esquina inferior izquierda. No usa OnGUI ni crea ventanas. Escucha UsesChanged; muestra solo el número actual, sin máximo ni etiquetas.

## Comprobación rápida

1. Confirma vida llena y contador 3 al empezar.
2. Entra y sal de la zona: la vida debe bajar a 75.
3. Pulsa Q fuera de la zona: la vida llega a 100 y el contador baja a 2.
4. Pulsa Q con vida llena: el contador debe seguir en 2.
5. Acércate al dummy: también debe bajar la vida, sin mostrar barra del dummy.
6. Permanece en contacto para comprobar el daño periódico y el reinicio al morir.

Los separadores vacíos desactivados siguen sin hijos. Los grupos funcionales son sus hermanos activos; no pongas contenido dentro de un separador.

