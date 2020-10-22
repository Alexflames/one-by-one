﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shotgun", menuName = "ScriptableObject/Weapon/Shotgun", order = 1)]
public class Shotgun : Pistol
{
    [SerializeField]
    private float arcAngle = 45;
    [SerializeField]
    private int shotNumber = 6;

    protected Shotgun() : base()
    {
        description = "Blast heads to problems";
    }

    protected override void CompleteAttack(CharacterShooting attackManager)
    {
        for (int i = 0; i < shotNumber; i++)
        {
            var bullet = PoolManager.GetPool(currentBulletPrefab, attackManager.weaponTip.position,
                             Quaternion.Euler(0, 0, attackManager.weaponTip.rotation.eulerAngles.z + 90 + GaussianRandom(0, Mathf.Pow(arcAngle, 0.7f))));
            BulletInit(bullet);
            var bulLife = bullet.GetComponent<BulletLife>();
            bulLife.speed += Random.Range(-bulLife.speed * 0.1f, bulLife.speed * 0.1f);
            bulLife.timeToDestruction += Random.Range(-bulLife.timeToDestruction * 0.6f, bulLife.timeToDestruction * 0.2f);
        }

        shootingEvents?.Invoke();
    }

    // because it spawns multiple bullets!
    public override float GunfirePower()
    {
        return base.GunfirePower() * shotNumber;
    }
}
