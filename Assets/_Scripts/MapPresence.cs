using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPresence : MonoBehaviour
{
    [Header("Inherited Settings")]
    [SerializeField] private Vector3 offset;

    // getters


    public ObjectTypes getType => objectType;
    public Vector2Int getPos => mapPos;


    public PlayerController getHarbouringPlayer => harbouringPlayer;


    // overwritten

    protected ObjectTypes objectType = ObjectTypes.Cube;

    // location

    protected Transform moveable;
    protected Vector2Int mapPos;


    private Vector3 desiredPos;
    private PlayerController harbouringPlayer = null;


    
    public void MoveTo(Vector2Int to)
    {
        mapPos = to;
        desiredPos = GameManager.instance.GetWorldPosByLoc(mapPos) + offset;
    }

    protected void UpdateRelocation()
    {
        if (!moveable) return;
        moveable.position = Vector3.Lerp(moveable.position, desiredPos, Time.deltaTime * GameManager.LERPSPEED);
    }


    public void SetHarbouringPlayer(PlayerController to) => harbouringPlayer = to;
}
