using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [Header("Waypoint Settings")]
    [SerializeField] private string id;
    [SerializeField] private string category = "spawn_point";

    public string Id => id;
    public string Category => category;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;
    
    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public List<Waypoint> connectedWaypoints = new List<Waypoint>();
}