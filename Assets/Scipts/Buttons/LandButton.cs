using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LandButton : MonoBehaviour
{
    [System.Serializable]
    public class TargetMap
    {
        public Tilemap tilemap;
        public PlayerController.PlayerColor currentColor = PlayerController.PlayerColor.Green;
    }

    [Header("Tilemaps to cycle")]
    [SerializeField] private TargetMap[] targets;

    [Header("Layer name prefix (must exist in Project Settings > Tags & Layers)")]
    [SerializeField] private string tilesLayerPrefix = "Tiles_"; // -> Tiles_Green, Tiles_Blue, Tiles_Pink, Tiles_Red

    [Header("Tile lists (matching order across colors!)")]
    [Tooltip("All GREEN tiles in the SAME order as the BLUE/PINK/RED lists.")]
    [SerializeField] private TileBase[] greenTiles;
    [Tooltip("All BLUE tiles in the SAME order as the GREEN/PINK/RED lists.")]
    [SerializeField] private TileBase[] blueTiles;
    [Tooltip("All PINK tiles in the SAME order as the others.")]
    [SerializeField] private TileBase[] pinkTiles;
    [Tooltip("All RED tiles in the SAME order as the others.")]
    [SerializeField] private TileBase[] redTiles;

    // quick maps: from a tile in one color -> the same tile in the next color
    private Dictionary<TileBase, TileBase> _greenToBlue, _blueToPink, _pinkToRed, _redToGreen;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";

    void Awake()
    {
        // Build swap dictionaries once
        _greenToBlue = BuildMap(greenTiles, blueTiles);
        _blueToPink  = BuildMap(blueTiles,  pinkTiles);
        _pinkToRed   = BuildMap(pinkTiles,  redTiles);
        _redToGreen  = BuildMap(redTiles,   greenTiles);
    }

    void Start()
    {
        // Ensure starting layers are correct
        foreach (var t in targets)
            ApplyLayer(t.tilemap, t.currentColor);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        CycleAll();
    }

    public void CycleAll()
    {
        foreach (var t in targets)
        {
            // 1) swap visuals on the tilemap
            SwapTilemapToNextColor(t.tilemap, t.currentColor);

            // 2) bump the logical color
            t.currentColor = Next(t.currentColor);

            // 3) move the tilemap to the new Tiles_* layer so collision pairs update
            ApplyLayer(t.tilemap, t.currentColor);
        }
    }

    // ---- helpers ----

    private static Dictionary<TileBase, TileBase> BuildMap(TileBase[] from, TileBase[] to)
    {
        var d = new Dictionary<TileBase, TileBase>();
        int n = Mathf.Min(from?.Length ?? 0, to?.Length ?? 0);
        for (int i = 0; i < n; i++)
        {
            if (from[i] != null && to[i] != null && !d.ContainsKey(from[i]))
                d[from[i]] = to[i];
        }
        return d;
    }

    private void SwapTilemapToNextColor(Tilemap map, PlayerController.PlayerColor fromColor)
    {
        if (map == null) return;

        var dict = MappingFor(fromColor);
        if (dict == null || dict.Count == 0) return;

        // Walk all placed cells and replace tiles using the map
        var bounds = map.cellBounds;
        map.BoxFill(Vector3Int.zero, null, 0, 0, 0, 0); // noop to make sure internal buffers exist

        foreach (var pos in bounds.allPositionsWithin)
        {
            var t = map.GetTile(pos);
            if (t != null && dict.TryGetValue(t, out var newTile))
                map.SetTile(pos, newTile);
        }

        map.RefreshAllTiles();
    }

    private Dictionary<TileBase, TileBase> MappingFor(PlayerController.PlayerColor from)
    {
        switch (from)
        {
            case PlayerController.PlayerColor.Green: return _greenToBlue;
            case PlayerController.PlayerColor.Blue:  return _blueToPink;
            case PlayerController.PlayerColor.Pink:  return _pinkToRed;
            case PlayerController.PlayerColor.Red:   return _redToGreen;
            default: return null;
        }
    }

    private void ApplyLayer(Tilemap map, PlayerController.PlayerColor color)
    {
        if (map == null) return;
        string layerName = tilesLayerPrefix + color;            // e.g. "Tiles_Red"
        int layer        = LayerMask.NameToLayer(layerName);
        if (layer == -1)
            Debug.LogError($"Layer '{layerName}' not found. Create it and wire up the Physics 2D matrix.");
        else
            map.gameObject.layer = layer;
    }

    private static PlayerController.PlayerColor Next(PlayerController.PlayerColor c)
    {
        int i = ((int)c + 1) % 4;
        return (PlayerController.PlayerColor)i;
    }
}
