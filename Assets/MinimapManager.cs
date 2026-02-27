using UnityEngine;
using System.Collections.Generic

[System.Serializable]
public class EntityData {
    public string type; // to icon types like "threat" or "victim", or "ducky"
    public Vector2 position;
}}

public class MinimapManager : MonoBehaviour
{
    [Header("UI Stuff")] //Title in Unity Inspector
    public Transform minimapContainer; //almost like a folder-ish for the icons
    public GameObject threatIcon; //red
    public GameObject victimIcon; //green
    public GameObject officerIcon; //blue

    private List<GameObject> activeIcons = new List<GameObject>(); //List to recall which dots are on the map

    void Start() {
        //Starts the fake data updater every second
        InvokeRepeating(nameof(UpdateWithMockData), 0f, 1f); //need to understand this line
    }
}
