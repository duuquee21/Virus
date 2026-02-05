using System.Collections.Generic;
using UnityEngine;

public class VirusEvolverController : MonoBehaviour
{
    public static VirusEvolverController instance;

    [System.Serializable]
    public class ShinyMilestone
    {
        public int requiredShinies;
        public List<GameObject> objects;
        [HideInInspector] public bool unlocked;
    }

    [Header("Milestones de Shinys")]
    public List<ShinyMilestone> milestones = new List<ShinyMilestone>();

    private int totalShiniesCaptured;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        // Apagar todo al inicio
        foreach (var m in milestones)
        {
            foreach (var obj in m.objects)
            {
                if (obj != null)
                {
                    SetActiveRecursive(obj, false);
                }
            }
        }
    }

    public void RegisterShiny()
    {
        totalShiniesCaptured++;
        CheckMilestones();
    }

    void CheckMilestones()
    {
        foreach (var m in milestones)
        {
            if (m.unlocked) continue;

            if (totalShiniesCaptured >= m.requiredShinies)
            {
                UnlockMilestone(m);
            }
        }
    }

    void UnlockMilestone(ShinyMilestone milestone)
    {
        milestone.unlocked = true;

        foreach (var obj in milestone.objects)
        {
            if (obj != null)
            {
                SetActiveRecursive(obj, true);
            }
        }

        Debug.Log("Milestone desbloqueado: " + milestone.requiredShinies + " shinys");
    }

    // 🔥 Activa / desactiva un objeto y TODOS sus hijos
    void SetActiveRecursive(GameObject obj, bool state)
    {
        obj.SetActive(state);

        foreach (Transform child in obj.transform)
        {
            SetActiveRecursive(child.gameObject, state);
        }
    }

    public int GetTotalShinies()
    {
        return totalShiniesCaptured;
    }
}
