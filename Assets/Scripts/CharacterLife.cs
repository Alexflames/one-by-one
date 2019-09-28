﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

public class CharacterLife : MonoBehaviour
{
    public static bool isDeath = false;
    [SerializeField]
    private GameObject ShadowObject;

    public void Death()
    {
        if (isDeath) return; // Already died

        LogicDeathBlock();
        VisualDeathBlock();
    }

    private void LogicDeathBlock()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        CharacterShooting shooting = GetComponent<CharacterShooting>();
        CharacterMovement movement = GetComponent<CharacterMovement>();
        if (movement)
            movement.enabled = false;
        //if (collider2D)
        //    collider2D.isTrigger = true;
        if (shooting)
            shooting.enabled = false;
        isDeath = true;
    }

    private void VisualDeathBlock()
    {
        lighter = GetComponentInChildren<Light2D>();
        glowIntense = lighter.intensity;
        StartCoroutine(StopGlow());

        GetComponentInChildren<Animator>().Play("Death");

        transform.eulerAngles = new Vector3(0, 0, 180);
        

        circleCollider.radius = 1.35f;
        GetComponent<Rigidbody2D>().mass = 10000;
    }

    private IEnumerator StopGlow()
    {
        while (glowFadeTime >= 0)
        {
            glowFadeTime -= Time.fixedDeltaTime;
            lighter.intensity = Mathf.Lerp(0, glowIntense, glowFadeTime);

            //ShadowObject.transform.localEulerAngles = 

            yield return new WaitForFixedUpdate();
        }
        circleCollider.radius = 0.2f;
    }

    public void Alive()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        CharacterShooting shooting = GetComponent<CharacterShooting>();
        CharacterMovement movement = GetComponent<CharacterMovement>();
        if (movement)
            movement.enabled = false;
        if (circleCollider)
            circleCollider.isTrigger = true;
        if (shooting)
            shooting.enabled = false;
        isDeath = false;
    }

    private Light2D lighter;
    private float glowIntense;
    private float glowFadeTime = 3;
    private CircleCollider2D circleCollider;
}
