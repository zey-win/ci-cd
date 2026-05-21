#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PrefabExporter
{
    [MenuItem("Tools/Export/Export Prefab (with deps report)")]
    public static void ExportPrefabWithDepsReport()
    {
        // ВПИШИ путь к префабу
        string prefabPath = "Assets/Prefabs/UI/WheelOfFortunePrefab.prefab";

        var prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Prefab not found: " + prefabPath);
            return;
        }

        var deps = AssetDatabase.GetDependencies(prefabPath, true);

        var inAssets = deps.Where(p => p.StartsWith("Assets/")).ToArray();
        var inPackages = deps.Where(p => p.StartsWith("Packages/")).ToArray();

        Debug.Log($"Deps total: {deps.Length}, Assets: {inAssets.Length}, Packages: {inPackages.Length}");

        if (inPackages.Length > 0)
        {
            Debug.LogWarning(
                "Some dependencies are in Packages/ and will NOT be included in unitypackage:\n" +
                string.Join("\n", inPackages)
            );
        }

        string outPath = EditorUtility.SaveFilePanel(
            "Export unitypackage",
            "",
            "Prefab_WithDeps.unitypackage",
            "unitypackage"
        );
        if (string.IsNullOrEmpty(outPath)) return;

        AssetDatabase.ExportPackage(
            inAssets,
            outPath,
            ExportPackageOptions.Interactive | ExportPackageOptions.Recurse
        );

        Debug.Log("Exported: " + outPath);
    }
}
#endif
