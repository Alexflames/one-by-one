﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraForLabirint : MonoBehaviour
{
    private GameObject cam;
    private GameObject currentRoom;
    private GameObject player;
    private bool followCamera = true;
    private float cameraBoundsLeft;
    private float cameraBoundsRight;
    private float cameraBoundsUp;
    private float cameraBoundsDown;

    public static CameraForLabirint instance;

    private void Start()
    {
        instance = this;
        cam = Camera.main.gameObject;
        player = GameObject.FindWithTag("Player");
    }

    private void Awake()
    {
    }

    private void Update()
    {
        if (followCamera) 
            CameraFollowUpdate();
        
    }

    public void ChangeRoom(GameObject room) {
        if (!followCamera)
            cam.transform.position = room.transform.position + 20 * Vector3.back;
        else 
            CameraFollowSetup(room);
    }

    void CameraFollowSetup(GameObject room) 
    {
        Tilemap[] tilemaps = room.GetComponentsInChildren<Tilemap>();
        float left; float right; float up; float down;
        foreach (Tilemap tilemap in tilemaps) {
            if (tilemap.tag == "Environment") { // to separete layer with walls
                Vector3Int tilePosition;
                left = Mathf.Infinity;
                right = -Mathf.Infinity;
                up = -Mathf.Infinity;
                down = Mathf.Infinity;
                for (int x = tilemap.origin.x; x < tilemap.size.x; x++)
                {
                    for (int y = tilemap.origin.y; y < tilemap.size.y; y++)
                    {
                        tilePosition = new Vector3Int(x, y, 0);
                        if (tilemap.HasTile(tilePosition))
                        {
                            if (tilemap.CellToWorld(tilePosition).x < left) left = tilemap.CellToWorld(tilePosition).x;
                            if (tilemap.CellToWorld(tilePosition).x > right) right = tilemap.CellToWorld(tilePosition).x;
                            if (tilemap.CellToWorld(tilePosition).y < down) down = tilemap.CellToWorld(tilePosition).y;
                            if (tilemap.CellToWorld(tilePosition).y > up) up = tilemap.CellToWorld(tilePosition).y;
                        }
                    }
                }
                cameraBoundsLeft = left+1; //+1 to shift from bot-left corner of tile to center, mb need to replace with something tile size related later
                cameraBoundsRight = right+1;
                cameraBoundsUp = up+1;
                cameraBoundsDown = down+1;
            }
        }
    }

    void CameraFollowUpdate(){
        cam.transform.position = player.transform.position - 20 * Vector3.forward;
        if (cam.GetComponent<Camera>().ViewportToWorldPoint(Vector3.zero).x < cameraBoundsLeft)
            cam.transform.position += Vector3.right * (cameraBoundsLeft - cam.GetComponent<Camera>().ViewportToWorldPoint(Vector3.zero).x);
        if (cam.GetComponent<Camera>().ViewportToWorldPoint(Vector3.one).x > cameraBoundsRight)
            cam.transform.position += Vector3.right * (cameraBoundsRight - cam.GetComponent<Camera>().ViewportToWorldPoint(Vector3.one).x);
        if (cam.GetComponent<Camera>().ViewportToWorldPoint(Vector3.one).y > cameraBoundsUp)
            cam.transform.position += Vector3.up * ( cameraBoundsUp - cam.GetComponent<Camera>().ViewportToWorldPoint(Vector3.one).y);
        if (cam.GetComponent<Camera>().ViewportToWorldPoint(Vector3.zero).y < cameraBoundsDown)
            cam.transform.position += Vector3.up * (cameraBoundsDown - cam.GetComponent<Camera>().ViewportToWorldPoint(Vector3.zero).y);
    }
}
