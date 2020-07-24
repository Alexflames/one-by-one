﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class EmpressBoss : BossEncounter
{
    [SerializeField] private GameObject[] explosiveBugs = null;
    [SerializeField] private GameObject[] beetles = null;
    [SerializeField] private AudioClip windSFX = null;
    [SerializeField] private AudioClip beetleSummon = null;
    [SerializeField] private AudioSource bossAttackSFXSource = null;
    [SerializeField] private Transform[] minibugSpawnPositions = null;

    public class EmpressFight : BossPhase
    {
        public EmpressFight(EmpressBoss bossData) : base(bossData)
        {
            phaseName = "Fight";
            BD = bossData;
            phaseType = PhaseType.HpBased;
            attackOrder = AttackOrder.Random;
            endHpPercentage = 0;
            attacks = new List<BossAttack>()
            {
                new SwarmAttack(BD, 5),
                new ForTheEmpress(BD, 4),
                new HiveMind(BD, 5),
                new WingsAttack(BD, 3.5f)
            };
        }

        EmpressBoss BD;
    }

    public class SwarmAttack : BossAttack
    {
        public SwarmAttack(EmpressBoss bossData, float attackLength, bool allowInterruption = true, bool ended = false) : base(bossData, attackLength, allowInterruption, ended)
        {
            BD = bossData;
            missileShoot = BD.GetComponent<PointMissileShoot>();
        }

        protected override void AttackStart()
        {
            base.AttackStart();
            shotsFired = 0;
            Debug.Log("Swarm attack: " + attackLength);
        }

        protected override void AttackUpdate()
        {
            base.AttackUpdate();
            if (attackTimeLeft < 1) return;
            if ((attackLength - attackTimeLeft) > (attackLength - 1) / shotsNeeded * shotsFired)
            {
                ShootFly();
            }
        }

        protected override void AttackEnd()
        {
            base.AttackEnd();
        }

        private void ShootFly()
        {
            Vector3 flySpawnPos = BD.minibugSpawnPositions[Random.Range(0, BD.minibugSpawnPositions.Length)].position;
            Vector3 flySpawnPosLocal = flySpawnPos - BD.transform.position;
            missileShoot.bulletSpawnOffset = flySpawnPosLocal;
            missileShoot.ForceAttack();
            shotsFired++;
        }

        int shotsFired;
        int shotsNeeded = 16;
        EmpressBoss BD;
        PointMissileShoot missileShoot;
    }

    public class ForTheEmpress : BossAttack
    {
        public ForTheEmpress(EmpressBoss bossData, float attackLength, bool allowInterruption = true, bool ended = false) : base(bossData, attackLength, allowInterruption, ended)
        {
            explosiveBugs = bossData.explosiveBugs;
            BD = bossData;
        }

        protected override void AttackStart()
        {
            bugsFired = 0;
            base.AttackStart();
        }

        protected override void AttackUpdate()
        {
            base.AttackUpdate();
            if (attackTimeLeft < 1) return;

            if ((attackLength - attackTimeLeft) > (attackLength - 1) / bugsCount * bugsFired)
            {
                lastBug = SummonBug();
            }
        }

        protected override void AttackEnd()
        {
            base.AttackEnd();
            if (lastBug) lastBug.GetComponent<MonsterLife>().Damage(null, 99999, true);
        }

        private GameObject SummonBug()
        {
            bugsFired++;
            int bugID = Random.Range(0, explosiveBugs.Length);
            var bug = PoolManager.GetPool(explosiveBugs[bugID], BD.transform.position, Quaternion.identity);
            var bugAIAgent = bug.GetComponent<AIAgent>();
            bugAIAgent.proximityCheckOption = new List<AIAgent.ProximityCheckOption>() { AIAgent.ProximityCheckOption.Always };
            return bug;
        }
        
        private int bugsFired = 0;
        private int bugsCount = 6;
        private GameObject[] explosiveBugs;
        private EmpressBoss BD;
        private GameObject lastBug;
    }

    public class HiveMind : BossAttack
    {
        public HiveMind(EmpressBoss bossData, float attackLength, bool allowInterruption = true, bool ended = false) : base(bossData, attackLength, allowInterruption, ended)
        {
            cameraShaker = bossData.GetComponent<ShakeCameraExternal>();
            beetles = bossData.beetles;
            BD = bossData;
        }

        protected override void AttackStart()
        {
            beetlesSummoned = false;
            base.AttackStart();
            cameraShaker.ShakeCamera();
            BD.bossAttackSFXSource.clip = BD.beetleSummon;
            AudioManager.Play("Beetle-summon", BD.bossAttackSFXSource);
        }

        protected override void AttackUpdate()
        {
            base.AttackUpdate();
            if (!beetlesSummoned) BD.ppBlur.blurSize.value = Mathf.Lerp(0, 0.035f, (attackLength - attackTimeLeft) / 0.75f);
            if (!beetlesSummoned && attackTimeLeft < attackLength - 0.75f) SummonBeetles();
            if (beetlesSummoned) BD.ppBlur.blurSize.value -= Time.deltaTime * 0.02f;
        }

        private void SummonBeetles()
        {
            beetlesSummoned = true;
            var beetlesAlive = BD.GetSpawnedMonsters().Count;
            var beetlesToSummonNow = Mathf.Min(beetlesToSummon, beetleSpawnLimit - beetlesAlive);
            for (int i = 0; i < beetlesToSummonNow; i++)
            {
                int beetleID = Random.Range(0, beetles.Length);
                Vector3 spawnPos = baseBossData.transform.position + new Vector3(Random.Range(-15, 15), Random.Range(-15, 15));
                var bug = PoolManager.GetPool(beetles[beetleID], spawnPos, Quaternion.identity);
                var bugAIAgent = bug.GetComponent<AIAgent>();
                bugAIAgent.proximityCheckOption = new List<AIAgent.ProximityCheckOption>() { AIAgent.ProximityCheckOption.Always };
                BD.AddMonster(bugAIAgent);
            }
        }

        ShakeCameraExternal cameraShaker;
        int beetlesToSummon = 3;
        int beetleSpawnLimit = 6;
        bool beetlesSummoned = true;
        GameObject[] beetles;
        EmpressBoss BD;
    }

    public class WingsAttack : BossAttack
    {
        public WingsAttack(EmpressBoss bossData, float attackLength, bool allowInterruption = true, bool ended = false) : base(bossData, attackLength, allowInterruption, ended)
        {
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
            boss = bossData.transform;
            BD = bossData;
        }

        protected override void AttackStart()
        {
            timer = 1.25f;
            BD.bossAttackSFXSource.clip = BD.windSFX;
            AudioManager.Play("WindSFX", BD.bossAttackSFXSource);
            base.AttackStart();
        }

        protected override void AttackUpdate()
        {
            base.AttackUpdate();
            timer -= Time.deltaTime;
            if (timer < 0) WingsPush();
        }

        private void WingsPush()
        {
            timer = 1.25f;
            player.velocity = (player.transform.position - boss.position).normalized * 20f;
            var monsters = BD.GetSpawnedMonsters();
            foreach (var monster in monsters)
            {
                monster.KnockBack((monster.transform.position - boss.position).normalized * 20f);
            }
        }

        private float timer = 1.25f;
        Rigidbody2D player;
        Transform boss;
        EmpressBoss BD;
    }

    protected override void Start()
    {
        audioSource = GetComponent<AudioSource>();
        bossPhases = new List<BossPhase>() { new EmpressFight(this) };
        ppBlur = GetComponentInChildren<PostProcessVolume>().profile.GetSetting<Blur>();
        hpManager = GetComponent<MonsterLife>();
        SetupDamageableSegments();

        StartCoroutine(StartNextFrame());
    }

    private IEnumerator StartNextFrame()
    {
        yield return new WaitForEndOfFrame();
        base.Start();
    }

    protected override void Update()
    {
        if (CharacterLife.isDeath) return;

        base.Update();
    }

    private void AddMonster(AIAgent monster)
    {
        spawnedMonsters.Add(monster);
    }

    private List<AIAgent> GetSpawnedMonsters()
    {
        spawnedMonsters.RemoveAll(monster => !monster);
        return spawnedMonsters;
    }

    private void SegmentDestroyed()
    {
        hpManager.Damage(null, hpManager.maxHP / 6f, true);
    }

    private void SetupDamageableSegments()
    {
        var segments = GetComponentsInChildren<VulnerableMonster>();
        foreach (var segment in segments)
        {
            segment.OnThisDead.AddListener(SegmentDestroyed);
        }
    }

    private AudioSource audioSource;
    private List<AIAgent> spawnedMonsters = new List<AIAgent>();
    private Blur ppBlur = null;
    private MonsterLife hpManager;
}
