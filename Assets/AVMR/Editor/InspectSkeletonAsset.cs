using System.Text;
using UnityEditor;
using UnityEngine;

public static class InspectSkeletonAsset
{
    public static void Run()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AVMR/Prefabs/sm_human_skeleton.fbx");
        if (asset == null)
        {
            Debug.LogError("skeleton asset missing");
            return;
        }

        var instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (instance == null)
        {
            Debug.LogError("could not instantiate skeleton asset");
            return;
        }

        var sb = new StringBuilder();
        Traverse(instance.transform, 0, sb);
        Debug.Log(sb.ToString());
        Object.DestroyImmediate(instance);
    }

    static void Traverse(Transform t, int depth, StringBuilder sb)
    {
        var indent = new string(' ', depth * 2);
        var mf = t.GetComponent<MeshFilter>();
        var mr = t.GetComponent<MeshRenderer>();
        var smr = t.GetComponent<SkinnedMeshRenderer>();
        sb.Append(indent).Append(t.name)
          .Append(" | MF:").Append(mf != null)
          .Append(" MR:").Append(mr != null)
          .Append(" SMR:").Append(smr != null)
          .AppendLine();
        foreach (Transform child in t)
            Traverse(child, depth + 1, sb);
    }
}
