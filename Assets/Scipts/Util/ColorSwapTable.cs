using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/Color Swap Table")]
public class ColorSwapTable : ScriptableObject
{
    // All four arrays must be the same length; index i across arrays are “the same tile” in different colors.
    public TileBase[] green;
    public TileBase[] blue;
    public TileBase[] pink;
    public TileBase[] red;

    // Returns the “next color” variant of the given tile (green->blue->pink->red->green). Null stays null.
    public TileBase Next(TileBase t)
    {
        if (!t) return null;
        int idx;

        if ((idx = IndexOf(green, t)) >= 0) return SafeGet(blue, idx);
        if ((idx = IndexOf(blue,  t)) >= 0) return SafeGet(pink, idx);
        if ((idx = IndexOf(pink,  t)) >= 0) return SafeGet(red,  idx);
        if ((idx = IndexOf(red,   t)) >= 0) return SafeGet(green,idx);

        // Tile not in the table -> leave it unchanged
        return t;
    }

    private static int IndexOf(TileBase[] arr, TileBase t)
    {
        if (arr == null) return -1;
        for (int i = 0; i < arr.Length; i++) if (arr[i] == t) return i;
        return -1;
    }
    private static TileBase SafeGet(TileBase[] arr, int i) => (arr != null && i >= 0 && i < arr.Length) ? arr[i] : null;
}