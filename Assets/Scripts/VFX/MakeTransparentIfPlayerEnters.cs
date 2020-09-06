﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeTransparentIfPlayerEnters : MonoBehaviour
{
    private bool shouldBeTransparent;
    private SpriteRenderer spriteRenderer;
    private Color startColor = Color.white;
    private Color destColor = new Color(1, 1, 1, 0.5f);
    private Color spriteColor;
    private float timeToTrans = 0.5f;
    private float timeToTransLeft = 0;
    private float timeToOpaq = 0.25f;
    private float timeToOpaqLeft = 0;

    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        startColor = spriteRenderer.color;
    }

    void Update()
    {
        if (shouldBeTransparent && timeToTransLeft > 0)
        {
            timeToTransLeft = Mathf.Clamp01(timeToTransLeft - Time.deltaTime);
            spriteRenderer.color = Color.Lerp(destColor, spriteColor, timeToTransLeft / timeToTrans);
        }
        else if (!shouldBeTransparent && timeToOpaqLeft > 0)
        {
            timeToOpaqLeft = Mathf.Clamp01(timeToOpaqLeft - Time.deltaTime);
            spriteRenderer.color = Color.Lerp(startColor, spriteColor, timeToOpaqLeft / timeToOpaq);
        } 
    }

    void OnTriggerEnter2D(Collider2D coll)
    {
        if (coll.CompareTag("Player"))
        {
            if (spriteRenderer.color.a == 1)
                startColor = spriteRenderer.color;
            shouldBeTransparent = true;
            timeToTransLeft = timeToTrans;
            spriteColor = spriteRenderer.color;
        }
    }

    void OnTriggerExit2D(Collider2D coll)
    {
        if (coll.CompareTag("Player"))
        {
            shouldBeTransparent = false;
            timeToOpaqLeft = timeToOpaq;
            spriteColor = spriteRenderer.color;
        }
    }
}
