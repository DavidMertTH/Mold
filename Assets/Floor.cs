using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Floor : MonoBehaviour
{
    public Tile[,] Tiles;
    public float tileSize;

    void Start()
    {
        Collider collider = gameObject.GetComponent<Collider>();
        Vector3 midpoint = collider.bounds.center;

        Tiles = new Tile[(int)collider.bounds.size.x, (int)collider.bounds.size.z];
        Vector3 corner = collider.bounds.min;
        for (int x = 0; x < Tiles.GetLength(0); x++)
        {
            for (int y = 0; y < Tiles.GetLength(1); y++)
            {
                // Vector3 position = new Vector3(corner.x + tileSize * x +tileSize/2, midpoint.y, corner.z + tileSize * y+tileSize/2);
                // GameObject go = 
                // Tiles[x, y] = tile;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (Tiles == null) return;

        for (int x = 0; x < Tiles.GetLength(0); x++)
        {
            for (int y = 0; y < Tiles.GetLength(1); y++)
            {
                Gizmos.DrawWireCube(Tiles[x, y].Position, new Vector3(Tiles[x, y].TileSize, Tiles[x, y].TileSize, Tiles[x, y].TileSize));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}