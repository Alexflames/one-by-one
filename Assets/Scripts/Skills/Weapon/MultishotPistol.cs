﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MultishotPistol", menuName = "ScriptableObject/Weapon/MultishotPistol", order = 1)]
public class MultishotPistol : Pistol
{
    [SerializeField]
    private float arcAngle = 45;
    [SerializeField]
    private int shotNumber = 3;

    protected override void CompleteAttack(CharacterShooting attackManager)
    {
        for (int i = 0; i < shotNumber; i++)
        {
            var bullet = PoolManager.GetPool(currentBulletPrefab, attackManager.weaponTip.position,
                Quaternion.Euler(0, 0, attackManager.weaponTip.rotation.eulerAngles.z + 90 + Mathf.Lerp(-arcAngle / 2, arcAngle / 2, i / (shotNumber - 1.0f))));
            BulletInit(bullet);
        }

        shootingEvents?.Invoke();
    }

    // because it spawns multiple bullets!
    public override float GunfirePower()
    {
        return base.GunfirePower() * shotNumber; 
    }
}
