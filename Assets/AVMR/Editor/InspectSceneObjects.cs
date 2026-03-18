using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InspectSceneObjects
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/AVMRAnatomyViewer.unity");
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var sb = new StringBuilder();
        foreach (var root in roots)
            Traverse(root.transform, 0, sb);
        Debug.Log(sb.ToString());
    }

    static void Traverse(Transform t, int depth, StringBuilder sb)
    {
        var indent = new string(' ', depth * 2);
        sb.Append(indent).Append(t.name);
        var comps = t.GetComponents<Component>();
        sb.Append(" | ");
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null) continue;
            sb.Append(comps[i].GetType().Name);
            if (i < comps.Length - 1) sb.Append(", ");
        }
        sb.AppendLine();
        foreach (Transform child in t)
            Traverse(child, depth + 1, sb);
    }
}
