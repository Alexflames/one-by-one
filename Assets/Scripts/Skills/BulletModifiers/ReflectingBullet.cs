﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RicochetBulletMod", menuName = "ScriptableObject/BulletModifier/Ricochet", order = 1)]
public class ReflectingBullet : BulletModifier
{
    public override void SpawnModifier(BulletLife bullet)
    {
        base.SpawnModifier(bullet);
        bullet.phasing = true;
    }

    public override void HitEnvironmentModifier(BulletLife bullet, Collider2D coll)
    {
        base.HitEnvironmentModifier(bullet, coll);

        Vector3 bulletPos = bullet.transform.position - bullet.transform.right * 0.1f;
        bulletPos.z = 0;
        Vector2 closestPoint = coll.ClosestPoint(bulletPos);
        var normal = (new Vector3(closestPoint.x, closestPoint.y) - bulletPos).normalized;

        Vector2 reflectDir = Vector2.Reflect(bullet.transform.right, normal);
        float rot = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
        bullet.GetComponent<Rigidbody2D>().rotation = rot;
    }
}
