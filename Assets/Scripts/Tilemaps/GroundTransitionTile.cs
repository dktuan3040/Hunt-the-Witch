using UnityEngine;
using UnityEngine.Tilemaps;

// Chooses original tileset edge sprites from the eight surrounding terrain cells.
[CreateAssetMenu(menuName = "Hunt the Witch/Tiles/Ground Transition")]
public sealed class GroundTransitionTile : Tile
{
    public enum TerrainKind { Dirt, Stone, Sand, Snow }
    public TerrainKind terrain;
    public bool joinOtherRoads;
    public Sprite[] spritesByMask = new Sprite[256];
    public TileBase[] compatibleTiles;
    public Sprite[] interiorVariants;
    private static readonly Vector3Int[] Neighbours = {
        new Vector3Int(-1,1,0), new Vector3Int(0,1,0), new Vector3Int(1,1,0),
        new Vector3Int(-1,0,0), new Vector3Int(1,0,0),
        new Vector3Int(-1,-1,0), new Vector3Int(0,-1,0), new Vector3Int(1,-1,0)
    };
    private bool Connects(TileBase other)
    {
        var transition = other as GroundTransitionTile;
        if (transition != null)
            return transition.terrain == terrain || (joinOtherRoads && terrain == TerrainKind.Stone
                && transition.terrain == TerrainKind.Dirt);
        if (compatibleTiles != null)
            foreach (var tile in compatibleTiles) if (other != null && tile == other) return true;
        return false;
    }
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);
        int mask = 0;
        for (int i = 0; i < Neighbours.Length; i++)
            if (Connects(tilemap.GetTile(position + Neighbours[i]))) mask |= 1 << i;
        if (spritesByMask != null && spritesByMask.Length == 256 && spritesByMask[mask] != null)
            tileData.sprite = spritesByMask[mask];
        if (mask == 255 && interiorVariants != null && interiorVariants.Length > 0)
        {
            uint hash = unchecked((uint)(position.x * 73856093 ^ position.y * 19349663));
            // Mostly plain centers, occasional texture variations without animated noise.
            if (hash % 5 == 0) tileData.sprite = interiorVariants[hash % (uint)interiorVariants.Length];
        }
        tileData.colliderType = ColliderType.None;
    }
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++) tilemap.RefreshTile(position + new Vector3Int(x,y,0));
    }
}
