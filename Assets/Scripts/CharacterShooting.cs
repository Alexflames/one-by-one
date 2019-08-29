﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterShooting : MonoBehaviour
{
    [SerializeField]
    private GameObject Bullet = null;

    [SerializeField]
    private float reloadTimeSec = 0.6f;

    [SerializeField]
    private float randomShootingAngle = 0;

    [SerializeField]
    private GameObject mouseCursorObj = null;

    private void Start()
    {
        mainCamera = Camera.main;
        Cursor.visible = false;
        GameObject.Instantiate(mouseCursorObj);
        shootSound = GetComponent<AudioSource>();
    }

    private void CmdShoot(Vector3 mousePos, Vector3 screenPoint)
    {
        var bullet = Instantiate(Bullet, transform.position, new Quaternion());
        if (shootSound)
        {
            shootSound.Play();
        }

        var offset = new Vector2(mousePos.x - screenPoint.x, mousePos.y - screenPoint.y);
        var angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        angle += Random.Range(-randomShootingAngle, randomShootingAngle);
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        bullet.transform.Translate(Vector2.right * 0.5f); 
    }

    private void Update()
    {
        if (reloadTimeLeft > 0)
        {
            reloadTimeLeft -= Time.deltaTime;
        }
        else if(Input.GetButton("Fire1"))
        {
            Vector3 mousePos = Input.mousePosition;
            var screenPoint = mainCamera.WorldToScreenPoint(transform.localPosition);
            CmdShoot(mousePos, screenPoint);
            reloadTimeLeft = reloadTimeSec;
        }
    }

    private float reloadTimeLeft = 0;
    private Camera mainCamera;
    private AudioSource shootSound;
}
