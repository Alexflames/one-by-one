﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseCursor : MonoBehaviour
{
    private Camera mainCam;
    private GameObject player;
    [SerializeField]
    private bool ShouldRotate = true;
    [SerializeField]
    private bool worldCoordinates = true;

    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main;
        player = GameObject.FindGameObjectWithTag("Player");
        transform.position = new Vector2(-1000, -1000);
    }

    // Update is called once per frame
    void Update()
    {
        if (Pause.Paused)
        {
            transform.position = new Vector3(-1337, -1337);
        }
        else
        {
            var mousePos = Input.mousePosition;
            if (worldCoordinates)
            {
                var screenPoint = mainCam.ScreenToWorldPoint(Input.mousePosition);
                screenPoint.z = 0;
                //Vector3 mousePos = Input.mousePosition;
                transform.position = screenPoint;
            }
            else {
                transform.position = mousePos;
            }

            if (ShouldRotate) RotateFromCharacter(mousePos);
        }
    }

    // Rotate cursor towards main character
    void RotateFromCharacter(Vector3 mousePos)
    {
        var characterPos = mainCam.WorldToScreenPoint(player.transform.localPosition);
        var offset = new Vector2(mousePos.x - characterPos.x, mousePos.y - characterPos.y);
        var angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }
}
