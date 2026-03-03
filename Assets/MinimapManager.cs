using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EntityData {
    public string type; // to icon types like "ducky" or "person"
    public Vector2 position;
}

public class MinimapManager : MonoBehaviour
{
    [Header("UI Stuff")] //Title in Unity Inspector
    public Transform minimapContainer; //almost like a folder-ish for the icons
    public GameObject duckyIconPrefab; //yellow
    public GameObject personIconPrefab; //blue

    private List<GameObject> activeIcons = new List<GameObject>(); //List to recall which dots are on the map

    void Start() {
        //Starts the fake data updater every second
        InvokeRepeating(nameof(UpdateWithMockData), 0f, 1f); //calls the pretend updater right now (0f) and repeat every second (1f)
    }

    void UpdateWithMockData()
    {
        // Fake test info from the server
        List<EntityData> mockData = new List<EntityData>
        {
            new EntityData { type = "ducky", position = new Vector2(0.3f,0.7f) },
            new EntityData { type = "person", position = new Vector2(0.5f,0.5f) },
            new EntityData { type = "person", position = new Vector2(0.6f,0.4f) }
        };
        UpdateMinimapIcons(mockData); //sends fake data to updater
    }

    public void UpdateMinimapIcons(List<EntityData> entities)
    {
        //rid of the old dots to update map for new positions / new dots
        foreach (var icon in activeIcons) Destroy(icon);
        activeIcons.Clear();

        //create new dot for all assets in list
        foreach (var entity in entities)
        {
            GameObject prefab = GetPrefabForType(entity.type);
            if (prefab == null) continue; // skips funcion if no corresponding dot

            GameObject icon = Instantiate(prefab, minimapContainer);
            RectTransform rt = icon.GetComponent<RectTransform>();

            //the following code puts the right dot in the right spot
            rt.anchoredPosition = new Vector2(
                entitiy.position.x * minimapContainer.GetComponent<RectTransform>().rect.width,
                entitiy.position.y * minimapContainer.GetComponent<RectTransform>().rect.height
            );

            activeIcons.Add(icon); //remember this new dot
        };
    }

    private GameObject GetPrefabForType(string type)
    {
        // assigns dots correct category based on word
        switch (type.ToLower())
        {
            case "ducky": return duckyIconPrefab;
            case "person": return personIconPrefab;
            default: return null; //If there is some other random bs that is making our code tweak
        }
    }
    //when server is ready add this to get real info
    //public void UpdateFromServer(string jsonData)
    //{
    //    //Turn words from server into a list of things like above
    //    //List<EntityData> realData = ... (parse jason)
    //    //UpdateMinimapIcons(realData);
    //}
}