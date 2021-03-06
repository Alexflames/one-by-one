﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantFireOnEnter : MonoBehaviour
{
    public GameObject firePrefab;
    private Room currentRoom;

    void Start()
    {
        currentRoom = GetComponentInParent<Room>();
        currentRoom.OnThisEnter.AddListener(Ignite);
    }

    void Ignite()
    {
        currentRoom.OnThisEnter.RemoveListener(Ignite);
        FireOnTilemap.StartFire(transform.position, firePrefab);
    }
}
