using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Cell<T>
{
    public Vector3 Min;
    public Vector3 Max;
    public List<T> Targets = new List<T>();
}