using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class EntityData
{
    public string type;      // Like "threat", "victim", or "officer"
    public Vector2 position; // Where on the map (0 to 1)
}

public class MinimapManager : MonoBehaviour
{
    [Header("UI Stuff")]
    public Transform MinimapContainer;         // Drag your UI Panel here
    public GameObject DuckyIcon;        // yellow dot prefab
    public GameObject PersonIcon;        // Blue dot prefab

    private List<GameObject> activeIcons = new List<GameObject>();

    void Start()
    {
        StartCoroutine(StartAfterFrame());
    }

    IEnumerator StartAfterFrame()
    {
        yield return null;                    // Wait one frame so Canvas is ready
        InvokeRepeating(nameof(UpdateWithMockData), 0f, 1f);
    }
    void UpdateWithMockData()
    {
        // Fake data to test — pretend server sent this
        List<EntityData> mockData = new List<EntityData>
        {
            new EntityData { type = "ducky", position = new Vector2(0.5f, 0.5f) },
            new EntityData { type = "person", position = new Vector2(0.6f, 0.6f) },
            new EntityData { type = "person", position = new Vector2(0.5f, 0.4f) }
        };

        UpdateMinimapIcons(mockData);
    }

   public void UpdateMinimapIcons(List<EntityData> entities)
    {
        if (MinimapContainer == null)
        {
            Debug.LogWarning("MinimapContainer is not assigned!");
            return;
        }

        // Remove old icons
        foreach (var icon in activeIcons)
            Destroy(icon);
        activeIcons.Clear();

        // Add new icons
        foreach (var entity in entities)
        {
            GameObject prefab = GetPrefabForType(entity.type);
            if (prefab == null) continue;

            GameObject icon = Instantiate(prefab, MinimapContainer);
            Debug.Log("Spawned " + entity.type + " at " + entity.position);

            RectTransform rt = icon.GetComponent<RectTransform>();

            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(
                    entity.position.x * MinimapContainer.GetComponent<RectTransform>().rect.width,
                    entity.position.y * MinimapContainer.GetComponent<RectTransform>().rect.height
                );
            }

            activeIcons.Add(icon);
        }
    }

    private GameObject GetPrefabForType(string type)
    {
        if (string.IsNullOrEmpty(type)) return null;

        switch (type.ToLower())
        {
            case "ducky":  return DuckyIcon;
            case "person":  return PersonIcon;
            default:        return null;
        }
    }
}