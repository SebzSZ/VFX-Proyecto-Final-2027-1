using FinalVFX.Combat;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponDefinition))]
public sealed class WeaponDefinitionEditor : Editor
{
    private bool _identity = true, _stats = true, _mount, _basic = true, _secondary = true, _ultimate = true;
    private int _delete = -1, _moveFrom = -1, _moveTo = -1;
    private bool _add;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        _delete = _moveFrom = _moveTo = -1;
        _add = false;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        if (Section(ref _identity, "Identidad del arma"))
        {
            Text("_displayName", "Nombre", "Nombre del arma utilizado en la presentación y los mensajes de combate.");
            Enum("_poseType", "Tipo de arma", "Selecciona las poses y la variante de habilidades: Sword = espada; Scythe = guadaña. También determina los campos de ultimate que se muestran.");
            ObjectField("_equippedPrefab", "Prefab equipado", "Objeto que PlayerEquipment instancia en WeaponSocket al equipar esta definición.", typeof(GameObject));
        }
        var pose = (WeaponDefinition.WeaponPoseType)P("_poseType").intValue;
        if (Section(ref _stats, "Estadísticas al equipar"))
        {
            Float("_maximumHealth", "Vida máxima", "Máximo de vida aplicado al equipar el arma. PlayerEquipment también restaura la vida al equiparla.", 1f);
            Float("_maximumStamina", "Stamina máxima", "Máximo de stamina aplicado y restaurado al equipar el arma.", 1f);
        }
        if (Section(ref _mount, "Colocación en WeaponSocket"))
        {
            Vector("_localPosition", "Posición local", "Desplazamiento del prefab respecto a WeaponSocket, en unidades de Unity.");
            Vector("_localEulerAngles", "Rotación local", "Rotación Euler del prefab respecto a WeaponSocket, en grados.");
            Vector("_localScale", "Escala local", "Escala del prefab equipado respecto al socket.");
        }
        if (Section(ref _basic, "Ataque básico · combo")) DrawBasic();
        if (Section(ref _secondary, "Habilidad secundaria"))
        {
            DrawAction("_secondary", false);
            Float("_secondaryCooldown", "Recarga (s)", "Tiempo desde el inicio de la secundaria hasta que puede volver a utilizarse.", 0f);
            Subheading("Impacto");
            DrawImpact("_secondaryImpact", P("_secondary.lockSeconds").floatValue);
            Note(pose == WeaponDefinition.WeaponPoseType.Scythe
                ? "El volumen circular se configura en PlayerHitVolumes → Secundaria de guadaña."
                : "El volumen de caja se configura en PlayerHitVolumes → Secundaria de espada.");
        }
        if (Section(ref _ultimate, "Ultimate")) DrawUltimate(pose);
        ApplyComboOperations();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBasic()
    {
        Float("_basicRestartDelay", "Espera al terminar (s)", "Espera adicional después de acabar un golpe sin enlazar otro, antes de poder iniciar un nuevo combo.", 0f);
        var steps = P("_basicSteps");
        EditorGUILayout.LabelField("Golpes del combo", steps.arraySize.ToString());
        Note("Cada golpe tiene una ventana de daño. Los golpes intermedios también tienen una ventana para pulsar el siguiente ataque y un instante de enlace.");
        for (int i = 0; i < steps.arraySize; i++)
        {
            var step = steps.GetArrayElementAtIndex(i);
            string root = step.propertyPath;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            step.isExpanded = EditorGUILayout.Foldout(step.isExpanded,
                "Golpe " + (i + 1) + (i == steps.arraySize - 1 ? " · final" : ""), true);
            using (new EditorGUI.DisabledScope(i == 0))
                if (GUILayout.Button(new GUIContent("↑", "Mover este golpe antes del anterior."), GUILayout.Width(26)))
                { _moveFrom = i; _moveTo = i - 1; }
            using (new EditorGUI.DisabledScope(i == steps.arraySize - 1))
                if (GUILayout.Button(new GUIContent("↓", "Mover este golpe después del siguiente."), GUILayout.Width(26)))
                { _moveFrom = i; _moveTo = i + 1; }
            if (GUILayout.Button(new GUIContent("Quitar", "Eliminar este golpe del combo. Se puede deshacer con Ctrl+Z."), GUILayout.Width(52)))
                _delete = i;
            EditorGUILayout.EndHorizontal();
            if (step.isExpanded)
            {
                ObjectField(root + ".soundEffect", "Efecto de sonido", "Se reproduce una vez al iniciar este ataque.", typeof(AudioClip));
                ObjectField(root + ".clip", "Animación", "El clip determina la duración del golpe básico. Si está vacío se utiliza Duración alternativa.", typeof(AnimationClip));
                var clip = P(root + ".clip").objectReferenceValue as AnimationClip;
                if (!clip)
                    Float(root + ".fallbackDuration", "Duración alternativa (s)", "Duración del golpe cuando no hay un clip asignado.", 0.01f);
                float duration = Mathf.Max(0.01f, clip ? clip.length : P(root + ".fallbackDuration").floatValue);
                Readout("Duración efectiva", Seconds(duration));
                Subheading("Daño y ventana de impacto");
                DrawImpact(root + ".impact", duration);
                if (i + 1 < steps.arraySize)
                {
                    Subheading("Enlace al siguiente golpe");
                    Fraction(root + ".open", "Aceptar entrada desde", "Fracción del golpe a partir de la cual se acepta la pulsación para encadenar el siguiente ataque.");
                    Fraction(root + ".close", "Aceptar entrada hasta", "Fracción en que cierra la ventana de entrada. En ejecución se limita entre la apertura y 1.");
                    Fraction(root + ".link", "Enlazar en", "Fracción en que comienza el siguiente golpe si se pulsó a tiempo. En ejecución se limita entre el cierre efectivo y 1.");
                    float open = P(root + ".open").floatValue;
                    float close = Mathf.Clamp(P(root + ".close").floatValue, open, 1f);
                    float link = Mathf.Clamp(P(root + ".link").floatValue, close, 1f);
                    Readout("Entrada efectiva", Seconds(open * duration) + " → " + Seconds(close * duration));
                    Readout("Enlace efectivo", Seconds(link * duration));
                }
            }
            EditorGUILayout.EndVertical();
        }
        if (GUILayout.Button(new GUIContent("Añadir golpe", "Añade un golpe con los valores iniciales de WeaponDefinition.BasicStep."))) _add = true;
        Note("El volumen del ataque básico se configura en PlayerHitVolumes → Básico compartido.");
    }

    private void DrawUltimate(WeaponDefinition.WeaponPoseType pose)
    {
        Subheading("Activación");
        DrawAction("_ultimate", true);
        Float("_ultimateCooldown", "Recarga (s)", "Recarga contada desde que empieza la activación. WeaponDefinition la limita como mínimo a Duración del efecto + 0.1 segundos.", 0f);
        Float("_ultimateEffect.duration", "Duración del efecto (s)", "Tiempo del beneficio, contado desde que termina la acción de activación de la ultimate.", 0.1f);
        Readout("Recarga efectiva", Seconds(Mathf.Max(P("_ultimateCooldown").floatValue, P("_ultimateEffect.duration").floatValue + 0.1f)));
        const string e = "_ultimateEffect.";
        if (pose == WeaponDefinition.WeaponPoseType.Sword)
        {
            Subheading("Espada · daño y escudo");
            Float(e + "swordBasicMultiplier", "Multiplicador de básicos", "Multiplica el daño de los ataques básicos mientras está activo el efecto de espada.", 1f);
            Float(e + "swordShieldPerBasic", "Escudo por impacto al jefe", "Escudo obtenido cuando un básico descuenta vida de GuardianHealth durante la ultimate. Se aplica el límite de escudo de HealthComponent.", 0f);
        }
        else if (pose == WeaponDefinition.WeaponPoseType.Scythe)
        {
            Subheading("Guadaña · recuperación de vida");
            Float(e + "scytheHealingPerBasic", "Curación por impacto al jefe", "Vida recuperada cuando un básico descuenta vida de GuardianHealth durante la ultimate, hasta la vida máxima.", 0f);
            Subheading("Aura · pulsos de daño");
            Float(e + "auraDamage", "Daño por pulso", "Daño del aura por objetivo válido en cada pulso.", 0f);
            Float(e + "auraInterval", "Intervalo entre pulsos (s)", "Tiempo entre pulsos. El primero ocurre después de este intervalo y se ejecuta como máximo un pulso por frame.", 0.05f);
            Subheading("Aura · volumen de contacto");
            Float(e + "auraRadius", "Radio (m)", "Radio horizontal del cilindro de daño del aura.", 0.01f);
            Float(e + "auraHeight", "Altura total (m)", "Altura total del cilindro, repartida por encima y por debajo de su centro.", 0.01f);
            Float(e + "auraCenterHeight", "Altura del centro (m)", "Desplazamiento vertical del centro del aura respecto a la posición del jugador.");
            Note("El Particle System del aura y sus capas objetivo se asignan en PlayerUltimateEffects del jugador.");
        }
        else Note("Selecciona Sword o Scythe en Tipo de arma para configurar el efecto específico de su ultimate.");
    }

    private void DrawAction(string root, bool ultimate)
    {
        ObjectField(root + ".soundEffect", "Efecto de sonido", "Se reproduce una vez al iniciar este ataque.", typeof(AudioClip));
        ObjectField(root + ".clip", "Animación", "Clip reproducido al iniciar la habilidad. Su longitud no determina el tiempo de bloqueo configurado.", typeof(AnimationClip));
        Float(root + ".lockSeconds", ultimate ? "Tiempo de activación (s)" : "Duración de la acción (s)",
            ultimate ? "Duración de la acción previa al efecto. Al terminar, PlayerCombat activa el beneficio de ultimate. Es independiente de la longitud del clip."
                : "Duración usada por PlayerCombat para el bloqueo y la ventana de impacto de la secundaria. Es independiente de la longitud del clip.", 0f);
        var clip = P(root + ".clip").objectReferenceValue as AnimationClip;
        if (clip) Readout("Longitud del clip", Seconds(clip.length));
    }

    private void DrawImpact(string root, float duration)
    {
        Float(root + ".damage", "Daño", "Daño base del impacto. El multiplicador de ultimate de espada se aplica a los básicos durante la ejecución.", 0f);
        Fraction(root + ".from", "Inicio del daño", "Fracción normalizada de la duración de la acción en la que empieza la ventana de impacto.");
        Fraction(root + ".until", "Fin del daño", "Fracción en que termina la ventana de impacto. En ejecución se utiliza al menos el valor de Inicio del daño.");
        float from = P(root + ".from").floatValue;
        float until = Mathf.Max(from, P(root + ".until").floatValue);
        Readout("Ventana de daño (s)", Seconds(from * duration) + " → " + Seconds(until * duration));
    }

    private void ApplyComboOperations()
    {
        var steps = P("_basicSteps");
        if (_delete >= 0) steps.DeleteArrayElementAtIndex(_delete);
        else if (_moveFrom >= 0) steps.MoveArrayElement(_moveFrom, _moveTo);
        else if (_add)
        {
            int index = steps.arraySize;
            steps.arraySize++;
            var step = steps.GetArrayElementAtIndex(index);
            step.FindPropertyRelative("clip").objectReferenceValue = null;
            step.FindPropertyRelative("soundEffect").objectReferenceValue = null;
            step.FindPropertyRelative("fallbackDuration").floatValue = 0.6f;
            step.FindPropertyRelative("open").floatValue = 0.25f;
            step.FindPropertyRelative("close").floatValue = 0.7f;
            step.FindPropertyRelative("link").floatValue = 0.8f;
            step.FindPropertyRelative("impact.damage").floatValue = 10f;
            step.FindPropertyRelative("impact.from").floatValue = 0.3f;
            step.FindPropertyRelative("impact.until").floatValue = 0.5f;
            step.isExpanded = true;
        }
    }

    // Controles explícitos: evitan repetir los antiguos Header del Inspector por defecto.
    // Solo escriben en SerializedProperty cuando el usuario realiza un cambio.
    private SerializedProperty P(string path) => serializedObject.FindProperty(path);
    private static GUIContent Label(string label, string tip) => new GUIContent(label, tip);
    private void Text(string path, string label, string tip)
    {
        var p = P(path); EditorGUI.BeginChangeCheck();
        string value = EditorGUILayout.TextField(Label(label, tip), p.stringValue);
        if (EditorGUI.EndChangeCheck()) p.stringValue = value;
    }
    private void Enum(string path, string label, string tip)
    {
        var p = P(path); EditorGUI.BeginChangeCheck();
        int value = EditorGUILayout.Popup(Label(label, tip), p.enumValueIndex, p.enumDisplayNames);
        if (EditorGUI.EndChangeCheck()) p.enumValueIndex = value;
    }
    private void Float(string path, string label, string tip, float minimum = float.NegativeInfinity)
    {
        var p = P(path); EditorGUI.BeginChangeCheck();
        float value = EditorGUILayout.FloatField(Label(label, tip), p.floatValue);
        if (EditorGUI.EndChangeCheck()) p.floatValue = Mathf.Max(minimum, value);
    }
    private void Fraction(string path, string label, string tip)
    {
        var p = P(path); EditorGUI.BeginChangeCheck();
        float value = EditorGUILayout.Slider(Label(label + " (0–1)", tip), p.floatValue, 0f, 1f);
        if (EditorGUI.EndChangeCheck()) p.floatValue = value;
    }
    private void ObjectField(string path, string label, string tip, System.Type type)
    {
        var p = P(path); EditorGUI.BeginChangeCheck();
        var value = EditorGUILayout.ObjectField(Label(label, tip), p.objectReferenceValue, type, false);
        if (EditorGUI.EndChangeCheck()) p.objectReferenceValue = value;
    }
    private void Vector(string path, string label, string tip)
    {
        var p = P(path); EditorGUI.BeginChangeCheck();
        Vector3 value = EditorGUILayout.Vector3Field(Label(label, tip), p.vector3Value);
        if (EditorGUI.EndChangeCheck()) p.vector3Value = value;
    }
    private static bool Section(ref bool expanded, string title)
    {
        EditorGUILayout.Space(8);
        expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
        return expanded;
    }
    private static void Subheading(string title)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
    private static void Readout(string name, string value) => EditorGUILayout.LabelField(name, value);
    private static void Note(string text) => EditorGUILayout.HelpBox(text, MessageType.Info);
    private static string Seconds(float value) => value.ToString("0.###") + " s";
}
