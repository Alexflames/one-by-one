﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [SerializeField]
    public float speed = 12f;
    
    private Animator anim;
    private Animator shadowAnim;
    private AudioSource audio;

    private void Start()
    {
        audio = GetComponent<AudioSource>();       
        var anims = GetComponentsInChildren<Animator>();
        anim = anims[0];
        shadowAnim = anims[1];
        mainCamera = Camera.main;
    }

    private void Update()
    {
        Movement();
        Rotation();
    }
    private void Rotation()
    {
        var mousepos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Quaternion rot = Quaternion.LookRotation(transform.position - mousepos, Vector3.forward);
        transform.rotation = rot;
        transform.eulerAngles = new Vector3(0, 0, transform.eulerAngles.z);
    }
    
    private void Movement()
    {       
        Vector2 direction = new Vector2();
        direction += new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (direction.magnitude > 1)
        {
            direction.Normalize();
        }
        if (anim != null)
        {
            if (direction.magnitude == 0) 
            {
                AudioManager.Pause("Walk", audio);
                anim.Play("HeroIdle");
                shadowAnim.speed = 0;
            }
            else if (AudioManager.isPlaying("Walk", audio) == false)
            {
                AudioManager.Play("Walk", audio);
                anim.Play("HeroWalking");
                shadowAnim.speed = 1;
            }        
        }
        transform.Translate(direction * speed * Time.deltaTime, Space.World);      
    }

    private Camera mainCamera = null;

}
