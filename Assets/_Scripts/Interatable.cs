using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interatable : MapPresence
{
    [Header("Main")]
    [SerializeField] private ObjectTypes type;

    private void Start()
    {
        objectType = type;
        moveable = transform;
    }

    private void Update()
    {
        UpdateRelocation();
    }
}
