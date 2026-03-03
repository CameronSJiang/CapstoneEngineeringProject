using UnityEngine;
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
    public Transform minimapContainer;         // Drag your UI Panel here
    public GameObject duckyIconPrefab;        // yellow dot prefab
    public GameObject personIconPrefab;        // Blue dot prefab

    private List<GameObject> activeIcons = new List<GameObject>();

    void Start()
    {
        // Start fake updates every second
        InvokeRepeating(nameof(UpdateWithMockData), 0f, 1f);
    }

    void UpdateWithMockData()
    {
        // Fake data to test — pretend server sent this
        List<EntityData> mockData = new List<EntityData>
        {
            new EntityData { type = "ducky", position = new Vector2(0.3f, 0.7f) },
            new EntityData { type = "person", position = new Vector2(0.6f, 0.4f) },
            new EntityData { type = "person", position = new Vector2(0.8f, 0.2f) }
        };

        UpdateMinimapIcons(mockData);
    }

    public void UpdateMinimapIcons(List<EntityData> entities)
    {
        // Remove old icons
        foreach (var icon in activeIcons)
        {
            Destroy(icon);
        }
        activeIcons.Clear();

        // Add new icons
        foreach (var entity in entities)
        {
            GameObject prefab = GetPrefabForType(entity.type);
            if (prefab == null) continue;

            GameObject icon = Instantiate(prefab, minimapContainer);
            RectTransform rt = icon.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2(
                entity.position.x * minimapContainer.GetComponent<RectTransform>().rect.width,
                entity.position.y * minimapContainer.GetComponent<RectTransform>().rect.height
            );

            activeIcons.Add(icon);
        }
    }

    private GameObject GetPrefabForType(string type)
    {
        switch (type.ToLower())
        {
            case "ducky":  return duckyIconPrefab;
            case "person":  return personIconPrefab;
            default:        return null;
        }
    }
}