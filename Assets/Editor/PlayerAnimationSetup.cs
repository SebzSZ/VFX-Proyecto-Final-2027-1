using UnityEditor;
using UnityEngine;

public static class PlayerAnimationSetup
{
    private const string Folder = "Assets/Models/PlayerFinal";

    [MenuItem("Tools/Player/Create Animation Setup")]
    private static void CreateSetup()
    {
        GameObject model =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{Folder}/Player.fbx");

        if (model == null)
        {
            Debug.LogError("No se encontró Player.fbx.");
            return;
        }

        var mask = new AvatarMask { name = "AM_WeaponArms" };

        // Este personaje utiliza una máscara de Transforms.
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);

        mask.AddTransformPath(model.transform, true);

        int enabledPaths = 0;

        for (int i = 0; i < mask.transformCount; i++)
        {
            string path = mask.GetTransformPath(i);

            bool isArm =
                ContainsBranch(path, "ORG-shoulder.L") ||
                ContainsBranch(path, "ORG-shoulder.R");

            mask.SetTransformActive(i, isArm);

            if (isArm)
                enabledPaths++;
        }

        if (enabledPaths == 0)
        {
            Object.DestroyImmediate(mask);
            Debug.LogError("No se encontraron las ramas de los brazos.");
            return;
        }

        var idle = new AnimationClip
        {
            name = "AN_IdleTemporary",
            frameRate = 30f
        };

        // Captura la pose original del asset, no una pose de Play Mode.
        foreach (Transform bone in
                 model.GetComponentsInChildren<Transform>(true))
        {
            if (bone == model.transform)
                continue;

            string path = AnimationUtility.CalculateTransformPath(
                bone, model.transform);

            Vector3 position = bone.localPosition;
            Quaternion rotation = bone.localRotation;
            Vector3 scale = bone.localScale;

            AddCurve(idle, path, "m_LocalPosition.x", position.x);
            AddCurve(idle, path, "m_LocalPosition.y", position.y);
            AddCurve(idle, path, "m_LocalPosition.z", position.z);

            AddCurve(idle, path, "m_LocalRotation.x", rotation.x);
            AddCurve(idle, path, "m_LocalRotation.y", rotation.y);
            AddCurve(idle, path, "m_LocalRotation.z", rotation.z);
            AddCurve(idle, path, "m_LocalRotation.w", rotation.w);

            AddCurve(idle, path, "m_LocalScale.x", scale.x);
            AddCurve(idle, path, "m_LocalScale.y", scale.y);
            AddCurve(idle, path, "m_LocalScale.z", scale.z);
        }

        idle.EnsureQuaternionContinuity();

        AnimationClipSettings settings =
            AnimationUtility.GetAnimationClipSettings(idle);

        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(idle, settings);

        // Si ejecutas el comando otra vez, crea copias sin sobrescribir.
        AssetDatabase.CreateAsset(
            mask,
            AssetDatabase.GenerateUniqueAssetPath(
                $"{Folder}/AM_WeaponArms.mask"));

        AssetDatabase.CreateAsset(
            idle,
            AssetDatabase.GenerateUniqueAssetPath(
                $"{Folder}/AN_IdleTemporary.anim"));

        AssetDatabase.SaveAssets();

        Selection.activeObject = mask;
        EditorGUIUtility.PingObject(mask);

        Debug.Log("Máscara de brazos e idle temporal creados.");
    }

    private static bool ContainsBranch(string path, string branch)
    {
        foreach (string segment in path.Split('/'))
        {
            if (segment == branch)
                return true;
        }

        return false;
    }

    private static void AddCurve(
        AnimationClip clip, string path, string property, float value)
    {
        clip.SetCurve(
            path,
            typeof(Transform),
            property,
            AnimationCurve.Constant(0f, 1f, value));
    }
}