// =============================================================================
// MinimapManager.cs — ARCH (Augmented Reality Crisis Helper)
// =============================================================================
// WHAT THIS FILE DOES:
//   This is the "client" end of the ARCH pipeline. It takes a list of entities
//   (threats/"ducky", people, officers...) with positions, and draws them as
//   icons on a 2D minimap in the Meta Quest HUD.
//
// THE FULL PIPELINE (where this script fits):
//
//   [Pi Camera Module 3] --> [Raspberry Pi 5: detection model] --> [JSON over
//   network (WebSocket planned)] --> [THIS SCRIPT on the Quest] --> [Minimap UI]
//
//   1. SENSE:   camera captures frames
//   2. DETECT:  a model on the Pi classifies what it sees (person / threat)
//   3. LOCATE:  convert pixel position -> position on the building floor plan
//   4. SEND:    serialize to JSON, push to the headset
//   5. DISPLAY: this script parses it and places icons (normalized 0..1 coords)

using UnityEngine;
using UnityEngine.Networking;       // UnityWebRequest — HTTP client. Only needed for the (commented-out) PollServer idea.
using System.Collections;           // IEnumerator — needed for coroutines.
using System.Collections.Generic;   // List<T>
using Newtonsoft.Json.Linq;         // JSON parsing. Not listed directly in Packages/manifest.json — it's pulled in
                                    // transitively by the Meta XR SDK (com.unity.nuget.newtonsoft-json). Not used yet.

// -----------------------------------------------------------------------------
// EntityData — the "data contract" between the Pi server and the headset.
// -----------------------------------------------------------------------------
// [System.Serializable] lets Unity (and JSON libraries) turn this into/out of
// text. A JSON message from the Pi would look something like:
//     [ { "type": "ducky",  "position": { "x": 0.5, "y": 1.0 } },
//       { "type": "person", "position": { "x": 0.6, "y": 0.6 } } ]
//
// KEY DESIGN DECISION — NORMALIZED COORDINATES (0 to 1):
//   (0,0) = bottom-left of the map, (1,1) = top-right. The server doesn't need
//   to know how big the minimap is in pixels, and the UI can be resized without
//   touching the server. This decouples the sensor side from the display side.

[System.Serializable]
public class EntityData
{
    public string type;      // Like "threat", "victim", or "officer"
                             // NOTE: the switch in GetPrefabForType only knows "ducky" and "person" —
                             // anything else is silently dropped. The server and client must agree on these strings.
    public Vector2 position; // Where on the map (0 to 1)
}

// MonoBehaviour = a script you attach to a GameObject in the Unity scene.
// In ARCH.unity this is on the "MinimapManager" object, and the fields below
// are wired up in the Inspector (MinimapContainer + the two icon prefabs).
public class MinimapManager : MonoBehaviour
{
    [Header("UI Stuff")]
    public Transform MinimapContainer;         // Drag my UI Panel here
                                               // The 400x400 RectTransform inside MinimapCanvas. Icons are spawned as its children.
    public GameObject DuckyIcon;        // yellow dot prefab
    public GameObject PersonIcon;        // Blue dot prefab
    // Prefab = a reusable template object (Assets/Prefabs/DuckyIcon.prefab, PersonIcon.prefab).
    // Instantiate() stamps out a copy at runtime.

    private List<GameObject> activeIcons = new List<GameObject>();
    // Tracks every icon currently on screen so they can be removed next update.

    // Start() is a Unity lifecycle method — runs once, before the first frame.
    void Start()
    {
        StartCoroutine(StartAfterFrame());
    }

    // COROUTINE: a function that can pause (yield) and resume on a later frame
    // without blocking the game. Unity-specific way to do "wait, then do X".
    IEnumerator StartAfterFrame()
    {
        yield return null;    // Wait one frame so Canvas is ready
                              // (UI layout — RectTransform sizes — is calculated during the first frame,
                              //  so rect.width/height could read 0 if we ran immediately.)
        InvokeRepeating(nameof(UpdateWithMockData), 0f, 0.7f);
        // InvokeRepeating: call UpdateWithMockData now (0s delay), then every 0.7s.
        // ~1.4 updates/sec. Fine for a demo; for "real-time" you'd want either a
        // faster rate or (better) event-driven updates from a WebSocket.
       // InvokeRepeating(nameof(PollServer), 0f, 0.7f);
    }

    // Fake data standing in for the Raspberry Pi until the hardware pipeline works.
    // IMPROVEMENT IDEA: define an interface (e.g. IEntitySource with a
    // "OnEntitiesUpdated" event) with two implementations — MockEntitySource and
    // WebSocketEntitySource — so you can switch between them without editing this class.
    void UpdateWithMockData()
    {
        // Fake data to test — pretend server sent this
        List<EntityData> mockData = new List<EntityData>
        {
            new EntityData { type = "ducky", position = new Vector2(0.5f, 1) },
            // y = 1 is the very top edge; since the icon pivot is centered, half this icon hangs off the map.
            new EntityData { type = "person", position = new Vector2(0.6f, 0.6f) },
            new EntityData { type = "person", position = new Vector2(0.5f, 0.4f) }
        };

        UpdateMinimapIcons(mockData);
    }

    // -------------------------------------------------------------------------
    // UpdateMinimapIcons — the core rendering logic.
    // Strategy: "clear and redraw". Destroy every old icon, spawn a new one for
    // each entity. Simple and always correct, but see the performance note below.
    // -------------------------------------------------------------------------
   public void UpdateMinimapIcons(List<EntityData> entities)
    {
        // Defensive check — if the Inspector reference wasn't set, warn instead of crashing.
        if (MinimapContainer == null)
        {
            Debug.LogWarning("MinimapContainer is not assigned!");
            return;
        }

        // Remove old icons
        // PERFORMANCE NOTE (important on a standalone headset):
        //   Destroy/Instantiate every tick creates garbage for the C# garbage
        //   collector. GC pauses cause dropped frames, and dropped frames in VR
        //   cause judder/motion sickness (Quest targets 72-120 fps, i.e. ~8-14 ms
        //   per frame). The standard fix is OBJECT POOLING: keep icons alive,
        //   keyed by entity id, and just move / show / hide them. That also lets
        //   you smoothly interpolate (Vector2.Lerp) an icon between updates
        //   instead of it teleporting every 0.7s.
        foreach (var icon in activeIcons)
            Destroy(icon);
        activeIcons.Clear();

        // Add new icons
        foreach (var entity in entities)
        {
            GameObject prefab = GetPrefabForType(entity.type);
            if (prefab == null) continue;   // unknown type -> skip

            // Instantiate(prefab, parent) spawns a copy as a child of the minimap panel,
            // so it inherits the panel's position/scale on screen.
            GameObject icon = Instantiate(prefab, MinimapContainer);
            Debug.Log("Spawned " + entity.type + " at " + entity.position);
            // Logging every icon every 0.7s is fine in the editor but is noisy and has a
            // real cost on-device. Strip or gate behind a debug flag for builds.

            RectTransform rt = icon.GetComponent<RectTransform>();

            // BUG: rt is used on the next three lines BEFORE the null check below.
            // If a prefab ever lacks a RectTransform this throws NullReferenceException.
            // Fix: move these three lines inside the `if (rt != null)` block.
            // Anchor (0,0) = measure position from the panel's bottom-left corner.
            // Pivot (0.5,0.5) = the icon's center is the point that gets placed.
            rt.anchorMin = new Vector2(0,0);
            rt.anchorMax = new Vector2(0,0);
            rt.pivot = new Vector2(0.5f, 0.5f);

            if (rt != null)
            {
                // THE COORDINATE TRANSFORM: normalized (0..1) -> pixel position on the panel.
                //   pixelX = normalizedX * panelWidth,  pixelY = normalizedY * panelHeight
                // e.g. (0.6, 0.6) on the 400x400 panel -> (240, 240) from bottom-left.
                // (Minor: GetComponent is called twice per icon per tick; caching the
                //  container's RectTransform once in Start() would be cleaner/faster.)
                rt.anchoredPosition = new Vector2(
                    entity.position.x * MinimapContainer.GetComponent<RectTransform>().rect.width,
                    entity.position.y * MinimapContainer.GetComponent<RectTransform>().rect.height
                );
            }

            activeIcons.Add(icon);
        }
    }

    // Maps the server's "type" string to the prefab to display.
    // ToLower() makes it case-insensitive ("Ducky" == "ducky").
    // A Dictionary<string, GameObject> or an enum would scale better as types are added
    // (threat, victim, officer, hazard zone...).
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
