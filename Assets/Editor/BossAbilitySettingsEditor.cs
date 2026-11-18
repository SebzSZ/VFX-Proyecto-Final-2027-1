using System.Collections.Generic;
using FinalVFX.Boss;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BossAbilitySettings))]
public sealed class BossAbilitySettingsEditor : Editor
{
    private static readonly Dictionary<string, string> Tips = new Dictionary<string, string>
    {
        { "Idle", "Clip del jefe detenido en el Blend Tree de locomoción." },
        { "Walk", "Clip de caminar mezclado según la velocidad horizontal del NavMeshAgent." },
        { "Stunned", "Clip reproducido mientras GuardianHealth mantiene aturdido al jefe." },
        { "WalkBlendSpeed", "Valor usado por el generador de recursos al crear el Blend Tree. En un Animator existente, ajusta también el Threshold de Walk directamente en el árbol." },
        { "Id", "Identificador usado por el selector de ataques. Cada habilidad debe tener un identificador distinto." },
        { "Clip", "Clip completo de preparación, ejecución y recuperación. Su velocidad se adapta a la suma de esas tres duraciones." },
        { "Preparation", "Segundos previos a la acción del ataque. El jugador puede anticiparlo; Blasfemia se eleva durante esta fase." },
        { "Execution", "Duración de la fase de acción. En Lanza Primordial controla el tiempo total de niebla y oleadas." },
        { "Recovery", "Segundos que el jefe permanece inmóvil después de la ejecución y antes de volver a decidir." },
        { "Damage", "Daño de un impacto aceptado por HealthComponent. En Lanza Primordial es el daño de cada golpe de un pilar." },
        { "HitInvulnerability", "Segundos mínimos de invulnerabilidad del jugador después de recibir este daño." },
        { "Radius", "Radio horizontal del área de impacto, en metros. Barrido utiliza solo la mitad frontal." },
        { "Height", "Altura del área de impacto medida desde la base del jefe, en metros." },
        { "TravelDistance", "Distancia horizontal máxima del desplazamiento de Barrido o del punto de caída de Blasfemia, en metros." },
        { "PushDistance", "Distancia solicitada de empuje radial del jugador. Las colisiones pueden acortar el recorrido." },
        { "ForceSeconds", "Duración del movimiento forzado del jugador: atracción en Ante Mi y empuje en En Garde o Lanza Primordial." },
        { "StunSeconds", "Tiempo de bloqueo de acciones del jugador después de terminar la atracción de Ante Mi." },
        { "RayDistance", "Alcance máximo del raycast lanzado desde RayOrigin, en metros." },
        { "PullStopDistance", "Separación horizontal buscada entre jugador y jefe al terminar la atracción." },
        { "DashFraction", "Fracción de Execution reservada al avance. El barrido se activa al llegar o cuando una colisión termina el avance." },
        { "RiseHeight", "Altura que asciende Blasfemia durante Preparation, en metros." },
        { "FogFadeIn", "Duración de la aparición suave de la niebla al iniciar Execution. La barrera ya está activa durante el fundido." },
        { "FogFadeOut", "Duración de la desaparición visual al terminar o interrumpirse el ataque. La barrera se desactiva al comenzar el fundido." },
        { "FogRadius", "Radio horizontal de la niebla y del límite que impide acercarse al jefe." },
        { "FogHeight", "Altura visual total de la niebla. La base se sitúa en la posición del jefe." },
        { "PillarNearRadius", "Distancia máxima desde el jefe para los pilares cercanos. El mínimo queda fuera de Fog Radius más Pillar Radius. El NavMesh y los límites de arena se siguen respetando." },
        { "NearPillarFraction", "Proporción de cada oleada situada cerca del jefe, redondeada hacia arriba. 0.75 con cinco pilares reserva cuatro cercanos; 1 reserva todos. El resto se reparte por toda la arena." },
        { "WaveInterval", "Segundos entre oleadas de pilares durante Execution." },
        { "PillarsPerWave", "Cantidad de pilares que se intenta colocar en cada oleada. Puede ser menor si faltan posiciones válidas." },
        { "PillarWarning", "Segundos de aviso circular antes de activar el fuego de cada pilar." },
        { "PillarLifetime", "Segundos que permanece activo el fuego después del aviso. El final de Execution retira los pilares restantes." },
        { "PillarRadius", "Radio horizontal del volumen de daño y del círculo de aviso de cada pilar." },
        { "PillarHeight", "Altura del volumen de daño de cada pilar y referencia para ajustar BreathSpeed del VFX." },
        { "PillarHitInterval", "Tiempo mínimo entre daños aceptados del mismo pilar sobre el jugador." }
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.LabelField("Animaciones de locomoción y aturdimiento", EditorStyles.boldLabel);
        foreach (string name in new[] { "Idle", "Walk", "Stunned", "WalkBlendSpeed" })
            Draw(serializedObject.FindProperty(name));

        EditorGUILayout.Space();
        var abilities = serializedObject.FindProperty("Abilities");
        abilities.isExpanded = EditorGUILayout.Foldout(abilities.isExpanded,
            "Habilidades (" + abilities.arraySize + ")", true);
        if (abilities.isExpanded)
        {
            int size = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Cantidad",
                "Número de entradas de la configuración. La implementación actual utiliza seis identificadores distintos."), abilities.arraySize));
            if (size != abilities.arraySize) abilities.arraySize = size;
            for (int i = 0; i < abilities.arraySize; i++)
            {
                var ability = abilities.GetArrayElementAtIndex(i);
                var id = (BossAttackId)ability.FindPropertyRelative("Id").intValue;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                ability.isExpanded = EditorGUILayout.Foldout(ability.isExpanded,
                    ObjectNames.NicifyVariableName(id.ToString()), true, EditorStyles.foldoutHeader);
                if (ability.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawFields(ability, "Id", "Clip", "Preparation", "Execution", "Recovery");
                    // Leer otra vez después de dibujar Id permite cambiar de grupo inmediatamente.
                    id = (BossAttackId)ability.FindPropertyRelative("Id").intValue;
                    float total = ability.FindPropertyRelative("Preparation").floatValue +
                        ability.FindPropertyRelative("Execution").floatValue + ability.FindPropertyRelative("Recovery").floatValue;
                    EditorGUILayout.LabelField("Duración total", total.ToString("0.###") + " s");
                    if (id == BossAttackId.AtaqueBasico)
                        EditorGUILayout.HelpBox("El daño, la invulnerabilidad y el volumen del ataque básico se configuran en DirectDamage y su BoxCollider, asignados en Melee Damage.", MessageType.Info);
                    else
                    {
                        Section("Daño");
                        DrawFields(ability, "Damage", "HitInvulnerability");
                    }
                    switch (id)
                    {
                        case BossAttackId.BarridoCelestial:
                            Section("Avance y semicírculo");
                            DrawFields(ability, "TravelDistance", "DashFraction", "Radius", "Height");
                            break;
                        case BossAttackId.AnteMi:
                            Section("Raycast, atracción y aturdimiento");
                            DrawFields(ability, "RayDistance", "PullStopDistance", "ForceSeconds", "StunSeconds");
                            break;
                        case BossAttackId.EnGarde:
                            Section("Área y empuje");
                            DrawFields(ability, "Radius", "Height", "PushDistance", "ForceSeconds");
                            break;
                        case BossAttackId.LanzaPrimordial:
                            Section("Expulsión y niebla");
                            DrawFields(ability, "PushDistance", "ForceSeconds", "FogRadius", "FogHeight", "FogFadeIn", "FogFadeOut");
                            Section("Distribución de pilares");
                            DrawFields(ability, "PillarNearRadius", "NearPillarFraction", "WaveInterval", "PillarsPerWave");
                            Section("Presentación y contacto de los pilares");
                            DrawFields(ability, "PillarWarning", "PillarLifetime", "PillarRadius", "PillarHeight", "PillarHitInterval");
                            break;
                        case BossAttackId.Blasfemia:
                            Section("Elevación, caída e impacto");
                            DrawFields(ability, "RiseHeight", "TravelDistance", "Radius", "Height");
                            break;
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    private static void Section(string title)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
    private static void DrawFields(SerializedProperty parent, params string[] names)
    {
        foreach (var name in names) Draw(parent.FindPropertyRelative(name));
    }
    private static void Draw(SerializedProperty property)
    {
        if (property == null) return;
        Tips.TryGetValue(property.name, out string tip);
        EditorGUILayout.PropertyField(property, new GUIContent(property.displayName, tip), true);
    }
}
