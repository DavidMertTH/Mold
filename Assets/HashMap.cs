using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;

public class HashMap<T>
{
    public float GridSize;
    public HashCell<T>[] Cells;
    public int MaxCells = 1024*32;

    public int GetHashIndex(Vector3 point)
    {
        return Math.Abs(Hash3((int ) (point.x / GridSize), (int)(point.y / GridSize),(int)(point.z / GridSize)) % MaxCells);
    }

    public void RemoveObject(T item, Vector3 key)
    {
        int hashIndex = GetHashIndex(key);
        if (Cells[hashIndex] == null) return;
        Cells[hashIndex].Remove(item);
    }

    public void AddObject(T item, Vector3 key)
    {
        int hashIndex = GetHashIndex(key);
        if (Cells[hashIndex] == null) Cells[hashIndex] = new HashCell<T>();

        if(Cells[hashIndex].Entrys.Contains(item))return;
        Cells[hashIndex].Add(item);
    }

    public List<T> GetSurroundingItems(Vector3 point)
    {
        List<T> entries = new List<T>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    int hashIndex = GetHashIndex(new Vector3(point.x + GridSize * x, point.y + GridSize * y,
                        point.z + GridSize * z));
                    if (Cells[hashIndex] == null) continue;
                    entries.AddRange(Cells[hashIndex].Entrys);
                }
            }
        }

        return entries;
    }

    public HashMap(float gridSize)
    {
        GridSize = gridSize;
        Cells = new HashCell<T>[MaxCells];
    }

    public Vector3 GetCornerPosition(Vector3 point)
    {
        return new Vector3(
            Mathf.Floor(point.x / GridSize) * GridSize,
            Mathf.Floor(point.y / GridSize) * GridSize,
            Mathf.Floor(point.z / GridSize) * GridSize
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Hash3(int x, int y, int z)
    {
        
        unchecked
        {
            uint xi = (uint)x * 73856093u;
            if (x < 0) xi = (uint)x * 32851413u;
            
            uint yi = (uint)y * 35135453u;
            if (x < 0) yi = (uint)y * 19349663u;
            
            uint zi = (uint)z * 689431324u;
            if (x < 0) zi = (uint)z * 83492791u;
            
            uint h = xi ^ yi ^ zi;

            h ^= h >> 16;
            h *= 0x7feb352d;
            h ^= h >> 15;
            h *= 0x846ca68b;
            h ^= h >> 16;
            return (int)h;
        }
    }


    public class HashCell<T>
    {
        public List<T> Entrys = new List<T>();
        public List<HashCell<T>> Neighbours = new List<HashCell<T>>();
        public void Remove(T item)
        {
            Entrys.Remove(item);
        }

        public void Add(T item)
        {
            Entrys.Add(item);
        }
        
    }
}