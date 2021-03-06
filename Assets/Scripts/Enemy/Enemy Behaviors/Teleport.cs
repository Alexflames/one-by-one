﻿using UnityEngine;
using System.Collections.Generic;

public class Teleport : TimedAttack
{
    [SerializeField]
    private float Scatter = 8f;
    [SerializeField]
    private float shakeAmp = 0.1f; // multiplier to shake distance

    protected override void Awake() 
    {
        base.Awake();
        maxspeedSaved = agent.maxSpeed;
        notWalkableMask = LayerMask.GetMask("Solid", "Abyss", "Ground");
    }

    protected override void CompleteAttack()
    {
        int i = 0;
        while (i < 5)
        {
            i++;
            float Xpos = Random.Range(-500, 500);
            float YPos = Random.Range(-500, 500);
            var vect = new Vector2(target.transform.position.x - Xpos, target.transform.position.y - YPos);
            vect.Normalize();
            vect *= Scatter;
            Vector3 NVector = new Vector3(vect.x, vect.y);
            bool inbounds;
            if (Labirint.instance && Labirint.currentRoom)
            {
                inbounds = Labirint.currentRoom.RectIsInbounds(target.transform.position.x + NVector.x, target.transform.position.y + NVector.y, 0, 0);
            }
            else inbounds = true;

            if (inbounds)
            {
                // We need teleport position not to be a solid object
                var canDrawDirectLine = !(Physics2D.Raycast(target.transform.position, NVector, NVector.magnitude, notWalkableMask));
                Debug.DrawRay(target.transform.position, NVector, Color.green, 1);
                //var hasWallSurrounding = Physics2D.OverlapCircle(target.transform.position + NVector, 2.5f, LayerMask.GetMask("Solid")) == null;
                if (canDrawDirectLine)
                {
                    var audio = GetComponent<AudioSource>();
                    AudioManager.Play("Blink", audio);
                    transform.position = target.transform.position + NVector;
                    GetComponent<MonsterLife>().FadeIn(0.5f);
                    StopKnockback();
                    EndShake();
                    break;
                }
            }
        }
        if (i == 5) EndShake(); //in case we can't find spot for teleport
    }

    private void EndShake() {
        agent.maxSpeed = maxspeedSaved;
        shakeMode = false;
    }

    protected override void AttackAnimation()
    {     
        agent.maxSpeed = 0f;
        shakeMode = true;
    }

    public override void CalledUpdate()
    {
        base.CalledUpdate();
        if (shakeMode) {
            Vector2 shift = new Vector2(Random.Range(-shakeAmp, shakeAmp), Random.Range(-shakeAmp, shakeAmp));
            gameObject.transform.Translate(shift, Space.World);
        }
    }

    private void StopKnockback()
    {
        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.isKinematic = true;
        rigidbody.isKinematic = false;
    }

    private float maxspeedSaved = 0f; //to hold maxSpeed when monster is stopped
    private bool shakeMode = false;

    private int notWalkableMask;
}
