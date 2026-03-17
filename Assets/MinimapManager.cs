using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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

    /*[Header("Server Stuff")]
    public string serverUrl = "https://..."; ONCE WE GET ACTUAL SERVER RUNNING CHANGE THIS
    */
    private List<GameObject> activeIcons = new List<GameObject>();

    void Start()
    {
        StartCoroutine(StartAfterFrame());
    }

    IEnumerator StartAfterFrame()
    {
        yield return null;    // Wait one frame so Canvas is ready
        InvokeRepeating(nameof(UpdateWithMockData), 0f, 0.7f);
       // InvokeRepeating(nameof(PollServer), 0f, 0.7f);
    }
    void UpdateWithMockData()
    {
        // Fake data to test — pretend server sent this
        List<EntityData> mockData = new List<EntityData>
        {
            new EntityData { type = "ducky", position = new Vector2(0.5f, 1) },
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

            rt.anchorMin = new Vector2(0,0);
            rt.anchorMax = new Vector2(0,0);
            rt.pivot = new Vector2(0.5f, 0.5f);

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