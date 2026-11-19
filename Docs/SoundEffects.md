# Efectos de sonido con pooling

## Configuración en Unity

1. Espera a que Unity importe y compile los scripts.
2. En `SCN_Arena`, crea un GameObject raíz activo llamado `SoundEffectPool` y añade el componente del mismo nombre. Mantenlo fuera de los objetos que se desactivan durante la selección de arma.
3. Deja `Initial Size` en **5** y `Volume` en **1**. Asigna en `Output Group` el grupo de efectos de `Assets/Audio/Mixers/MIX_Game.mixer`, para que los ajustes de volumen y el tratamiento de pausa del mixer afecten a estos sonidos.
4. Selecciona `Assets/Data/SO_Sword.asset`. En **Ataque básico · combo**, despliega cada golpe y asigna **Efecto de sonido** (`sword attack.mp3`). En **Habilidad secundaria**, asigna `sword secondary.mp3`. En **Ultimate → Activación**, asigna el clip que quieras para iniciar la ultimate.
5. Repite en `SO_Scythe.asset` con `scythe attack.mp3`, `scythe secondary.mp3` y el clip de ultimate elegido. Cada golpe puede tener un sonido diferente. Un campo vacío simplemente no reproduce sonido.
6. Selecciona el objeto con `ArenaDragonController`. En **Audio**, asigna `dragon roar.mp3` a **Attack Decision Sound** y `dragon fire.mp3` a **Fire Loop Sound**. Conviene que el clip de fuego tenga un inicio y final compatibles para repetirse sin un corte audible.
7. Guarda los assets y la escena. Comprueba que hay un AudioListener activo, normalmente en la cámara.

Los clips se asignan manualmente: los cambios de código no modifican tus escenas ni las referencias de los assets.

## Llamada desde cualquier clase

```csharp
using FinalVFX.Audio;
using UnityEngine;

public sealed class ExampleSound : MonoBehaviour
{
    [SerializeField] private AudioClip _clip;

    public void PlaySound()
    {
        SoundEffectPool.Instance.Play(_clip);
    }
}
```

No necesitas añadir un AudioSource al jugador ni al dragón. Los sonidos son 2D: no disminuyen con la distancia a la cámara.

Para un sonido continuo, conserva el identificador de reproducción:

```csharp
private SoundEffectPool.Handle _sound;

// Al empezar:
_sound = SoundEffectPool.Instance.Play(_clip, loop: true);

// Al terminar o desactivar al emisor:
_sound.Stop();
```

Detén un bucle antes de reemplazar su identificador por otro. Llamar varias veces a `Stop()` es seguro; un identificador viejo nunca detiene un nuevo sonido que reutilice la misma fuente.

## Funcionamiento y momentos de reproducción

- En `Awake`, el singleton crea cinco hijos con GameObject y AudioSource desactivados. Al reproducir, asigna un clip y activa una fuente libre. Al terminar, borra el clip y la desactiva para reutilizarla.
- Cinco es el tamaño inicial, no un límite: si todos están ocupados, se crea una fuente adicional, que se conserva para siguientes reproducciones.
- El básico suena al iniciar cada golpe real, no al pulsar para encolar el siguiente. Secundaria y ultimate suenan al comenzar la acción aceptada; pulsar durante cooldown no dispara audio. La ultimate suena al iniciar su animación, antes de que se active el beneficio.
- El aviso del dragón suena una vez al seleccionar una zona válida y comenzar la aproximación. Si después se cancela esa aproximación, el aviso ya emitido termina normalmente.
- El fuego comienza junto con `breath.Play()`, se repite durante la emisión y se detiene en `fireEnd`, al cancelar/terminar el ataque o al desactivar el dragón, incluida la muerte del jefe.
- Con `Time.timeScale = 0`, las fuentes se pausan y se reanudan al continuar. La pausa del AudioListener tampoco devuelve fuentes ocupadas al pool.
- El pool pertenece a la escena y se destruye al descargarla. Añade uno configurado en cada escena que necesite efectos. Si falta, la primera llamada crea uno automáticamente con cinco fuentes y sin mixer asignado; para precargar desde el inicio y respetar el volumen SFX, usa el objeto configurado en la escena.

## Verificación en Play Mode

1. Antes de atacar, expande el pool: debe tener cinco hijos inactivos.
2. Ejecuta combos, secundaria y ultimate con ambas armas. Confirma un sonido por inicio de acción y ninguno por entradas rechazadas.
3. Superpón más de cinco sonidos: los anteriores deben continuar y el pool debe crecer. Al acabar, sus hijos deben quedar inactivos y sin clip.
4. Comprueba el aviso al elegir zona, el bucle durante el fuego y su detención al terminar, interrumpirse o morir el jefe.
5. Pausa durante un sonido largo y reanuda. Ajusta el volumen SFX y verifica que controla estos efectos.
6. Regresa al menú y vuelve a la arena: no debe quedar audio de la escena anterior.

Se verificó la compilación C# de scripts de ejecución e Inspector con las referencias locales del proyecto. La escucha, el balance y la sincronización visual deben comprobarse en Play Mode.
