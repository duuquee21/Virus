using UnityEngine;
using System.Collections;

public class SkillTreeManager : MonoBehaviour
{
    void OnEnable()
    {
        StartCoroutine(RebuildTree());
    }

    IEnumerator RebuildTree()
    {
        yield return null;

        var nodes = FindObjectsOfType<SkillNode>(true);

        // 1️⃣ Cargar estados
        foreach (var node in nodes)
            node.LoadNodeState();

        // 2️⃣ Evaluar todos
        foreach (var node in nodes)
            node.CheckIfShouldShow();
    }
}
