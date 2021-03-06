﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LizardWaveFace : Align
{
    [SerializeField] protected float wavePeriod = 2f; 
    [SerializeField] protected float waveAmp = 40f;
    [SerializeField] private float behaviourBlockTime = 0.5f; // without this may get stuck

    protected override void Start()
    {
        base.Start();
        wavePhase += Random.Range(0, wavePeriod);
    }

    public override float GetRotation(float amp = 0)
    {
        if (isActive)
        {
            Vector2 direction = target.transform.position - transform.position;
            if (direction.magnitude > 0.0f)
            {
                float targetOrientation = Mathf.Atan2(direction.x, direction.y);
                targetOrientation *= Mathf.Rad2Deg;
                targetOrientation += WaveFluctuation();
                base.targetOrientation = targetOrientation;
            }
        }

        return base.GetRotation(targetOrientation);
    }

    //delta orientation from line of sight to wave trajectory
    protected float WaveFluctuation() {
        if (Mathf.Max(behaviourBlockTime -= Time.deltaTime, 0) > 0) return 0;

        wavePhase += Time.deltaTime / wavePeriod;
        if (wavePhase > 1) wavePhase -= 1;
        return Mathf.Sin(wavePhase * 2 * Mathf.PI) * waveAmp;
    }
    
    private float wavePhase = 0f;
}
