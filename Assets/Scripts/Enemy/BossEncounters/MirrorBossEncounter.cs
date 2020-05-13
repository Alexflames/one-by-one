﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MirrorBossEncounter : BossEncounter
{
    [HideInInspector] public Transform player;
    public Transform bossInstance;

    [Header("General references")]
    public GameObject bossPrefab = null;
    public Transform bossSpawnPosition = null;
    public GameObject bossSpawnEffect = null;
    [Header("Phase 1 Attack 1 & 3")]
    public GameObject explosionProjectile = null;
    public Transform[] phase1MovePositions = null;
    [HideInInspector] public List<Transform> avoidBTP = new List<Transform>(); // backtrack projectiles
    [Header("Phase 1 Attack 2 & 4")]
    public GameObject miniBombProjectile = null;
    public ZoneScript miniBombTeleportPos = null;
    [HideInInspector] public List<EnemyBulletLife> miniBombBullets = new List<EnemyBulletLife>();
    [Header("Phase 1 Attack 5")]
    public GameObject ellipseBulletPrefab = null;
    public Transform roomCenter = null;
    public Vector2 distanceFromCenterToUpRight = new Vector2(10, 10);
    public Chapter1MirrorBulletInfuser mirrorBulletInfuser = null;
    [Header("Pre-phase 2")]
    public SpriteRenderer glassEffect = null;
    [Header("Phase 2")]
    public ZoneScript[] spawnZones = null;
    public GameObject shootingMonsterPrefab = null;
    public GameObject zombiePrefab = null;
    public GameObject ghostPrefab = null;
    public GameObject teleportMonsterPrefab = null;
    public GameObject chaosMonsterPrefab = null;
    public GameObject tankPrefab = null;
    public GameObject ricochetPrefab = null;
    [Header("Post-phase 2")]
    public GameObject glassShards = null;
    public GameObject mirrorCracks = null;
    [Header("Phase 3")]
    public ZoneScript movePosition = null;
    public Color mirrorColor = Color.white;
    public GameObject outrageBullet = null;
    [HideInInspector] public List<MonsterLife> spawnedMonsters = new List<MonsterLife>();

    private class SpawnBossAttack : BossAttack
    {
        public SpawnBossAttack(BossEncounter bossData, float attackLength, bool allowInterruption = true, bool ended = false) : base(bossData, attackLength, allowInterruption, ended)
        {
            BD = bossData as MirrorBossEncounter;
        }

        protected override void AttackStart()
        {
            AudioManager.PauseMusic();
            Instantiate(BD.bossSpawnEffect, BD.bossSpawnPosition.position, Quaternion.identity);
        }

        protected override void AttackEnd()
        {
            AudioManager.PlayMusic(BD.GetComponent<AudioSource>());
            BD.bossInstance.gameObject.SetActive(true);
            BD.bossInstance.position = BD.bossSpawnPosition.position;
            BD.bossInstance.GetComponent<MonsterLife>().SetMinHpPercentage(0.35f);
        }

        private MirrorBossEncounter BD;
    }

    [System.Serializable]
    private class ExplosionAttack : BossAttack
    {
        public ExplosionAttack(BossEncounter bossData, float attackLength, int projectilesCount, float waitBefore = 0, bool returnBack = true, bool allowInterruption = true, bool ended = false) 
            : base(bossData, attackLength, allowInterruption, ended)
        {
            this.projectilesCount = projectilesCount; 
            this.BD = bossData as MirrorBossEncounter;
            initialWait = waitBefore;
            wait = waitBefore;
        }

        protected override void AttackStart()
        {
            projectilePrefab = BD.explosionProjectile;
            normalBulletSpeed = projectilePrefab.GetComponent<EnemyBulletLife>().BulletSpeed;
            bossInstance = BD.bossInstance;

            bossSprite = bossInstance.GetComponentInChildren<SpriteRenderer>().gameObject;

            bulletMagnetTL = timeToBulletMagnet;

            base.AttackStart();
        }

        protected override void AttackUpdate()
        {
            base.AttackUpdate();
            if (wait <= 0)
            {
                wait = Mathf.Infinity;
                startPosition = bossInstance.transform.position;
                ExplodeTowardsPlayer();
                ExplodeCircle();
            }
            if (endPosition != Vector3.zero)
            {
                //AdjustBulletSpeed();
                if (bulletMagnetTL > 0)
                {
                    bulletMagnetTL -= Time.deltaTime;
                    if (bulletMagnetTL <= 0)
                    {
                        MagnetAvoidBTPToBoss();
                    }
                }
                else if (magnetTime > 0)
                {
                    magnetTime -= Time.deltaTime;
                    if (magnetTime <= 0)
                    {
                        SplitBullets();
                    }
                }
                else
                {
                    //AdjustBulletSpeed();
                    bossInstance.transform.position = 
                        Vector3.Lerp(endPosition, startPosition, 
                            attackTimeLeft / (attackLength - initialWait - startingMagnetTime - timeToBulletMagnet));
                }
            }
            wait -= Time.deltaTime;
        }

        private void MagnetAvoidBTPToBoss()
        {
            magnetTime = startingMagnetTime;
            print("Magnetting back");
            foreach (var bullet in bullets)
            {
                bullet.BulletSpeed *= -1;
            }
        }

        private void SplitBullets()
        {
            print("Splitting bullets");
            var newBullets = new List<GameObject>();
            foreach (var bullet in bullets)
            {
                bullet.BulletSpeed *= -1f;
                var newBullet = Instantiate(bullet.gameObject, bullet.transform.position, Quaternion.Euler(0, 0, bullet.transform.eulerAngles.z + 60f));
                newBullets.Add(newBullet);
                bullet.transform.rotation = Quaternion.Euler(0, 0, bullet.transform.eulerAngles.z - 60);
            }
            foreach (var bullet in newBullets)
            {
                InitializeBullet(bullet);
            }
        }

        private void AdjustBulletSpeed()
        {
            var timeParamter = attackTimeLeft / (attackLength - initialWait);
            //var factor = normalBulletSpeed * Mathf.Max(minSpeed, (1 - Mathf.Pow(Mathf.InverseLerp(stopTimeRange.x, stopTimeRange.y, timeParamter), 3)));
            var factor = normalBulletSpeed * Mathf.Max(minSpeed, Mathf.Pow(timeParamter, 3));
            foreach (var bullet in bullets)
            {
                bullet.BulletSpeed = factor;
                print(factor);
            }
        }

        private void ExplodeTowardsPlayer()
        {
            Vector3 toPlayer = bossInstance.transform.position - BD.player.position;
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            for (int i = 0; i < projectilesCount; i++)
            {
                var randomAngle = Random.Range(-angleVariation, angleVariation);
                var bullet = Instantiate(projectilePrefab, bossInstance.position, Quaternion.Euler(0, 0, angle + 180 + randomAngle));
                InitializeBullet(bullet);
            }

            ChooseEndMovePosition();
        }

        private void ExplodeCircle()
        {
            float angleOffset = Random.Range(0, 90f);
            for (int i = 0; i < circleProjectileCount; i++)
            {
                Instantiate(projectilePrefab, bossInstance.position, Quaternion.Euler(0, 0, angleOffset + (360f * i) / circleProjectileCount));
            }
        }

        private void InitializeBullet(GameObject bullet)
        {
            BD.avoidBTP.Add(bullet.transform);
            var bulletLife = bullet.GetComponent<EnemyBulletLife>();
            bulletLife.BulletSpeed += Random.Range(-bulletSpeedVariation, bulletSpeedVariation);
            bullets.Add(bulletLife);
        }

        private void ChooseEndMovePosition()
        {
            // TODO: Сделать хитрее, чтобы не брались дистанции дальше X метров и ближе Y метров
            int counter = 0;
            while (counter < 10)
            {
                endPosition = BD.phase1MovePositions[Random.Range(0, BD.phase1MovePositions.Length)].position;
                float distance = Vector3.Distance(endPosition, BD.player.position);
                if (distance >= 5 && distance <= 17) {
                    break;
                }
                Debug.Log("Reconsidering");
                counter++;
            }
        }

        private void BossFadeAway()
        {

        }

        private void BossAppear()
        {

        }

        private float initialWait = 0;
        private float wait = 0;
        private int projectilesCount = 0;
        private int circleProjectileCount = 8;
        private GameObject projectilePrefab;
        private MirrorBossEncounter BD;
        private Transform bossInstance;
        private List<Transform> bulletsTransform = new List<Transform>();
        private List<EnemyBulletLife> bullets = new List<EnemyBulletLife>();
        private Vector3 startPosition;
        private Vector3 endPosition;
        
        private float bulletMagnetTL = 0f;
        private float timeToBulletMagnet = 0.2f;
        private float startingMagnetTime = 0.15f;
        private float magnetTime = 0;

        private const float angleVariation = 75f;
        private const float bulletSpeedVariation = 3f;
        private float normalBulletSpeed = 0;
        private float minSpeed = 0.2f;
        private Vector2 stopTimeRange = new Vector2(0.5f, 1.5f);

        private GameObject bossSprite;
        //private float appearTimeLeft = 0.3f;
        //private float fadeTimeLeft = 0.3f;
    }

    protected class MultibombAttack : BossAttack
    {
        public MultibombAttack(BossEncounter bossData, float attackLength, float additionalSpeed = 0, bool allowInterruption = true, bool ended = false) 
            : base(bossData, attackLength, allowInterruption, ended)
        {
            BD = bossData as MirrorBossEncounter;
            miniProjectilePrefab = BD.miniBombProjectile;
            player = BD.player;
            teleportZone = BD.miniBombTeleportPos;
            teleportZone.UseZone();
            projectileAdditionalSpeed = additionalSpeed;
        }

        protected override void AttackStart()
        {
            miniBombBullets = BD.miniBombBullets;
            miniBombBullets.Clear();
            base.AttackStart();
            bossInstance = BD.bossInstance;
        }

        protected override void AttackUpdate()
        {
            bombPlaceTL -= Time.deltaTime;
            if (bombPlaceTL <= 0)
            {
                MinibombExplosion();
                bombPlaceTL = bombPlacePeriod;
            }

            UpdateIncreaseSpeed();

            base.AttackUpdate();
        }

        protected override void AttackEnd()
        {
            foreach (var bullet in miniBombBullets)
            {
                Vector3 toPlayer = bullet.transform.position - BD.player.position;
                float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, 270 + angle);
                bullet.BulletSpeed = 25f;
            }
        }

        protected void MinibombExplosion()
        {
            for (int i = 0; i < bombProjectileCount; i++)
            {
                var angle = 360 / bombProjectileCount * i;
                var bullet = Instantiate(miniProjectilePrefab, bossInstance.position, Quaternion.Euler(0, 0, angle));
                var bulletComp = bullet.GetComponent<EnemyBulletLife>();
                bulletComp.BulletSpeed += projectileAdditionalSpeed;
                miniBombBullets.Add(bulletComp);
            }
            RandomTeleportInZone();
        }

        protected void RandomTeleportInZone()
        {
            int triesLimit = 0;
            while (triesLimit < 15)
            {
                Vector2 newPosition = teleportZone.RandomZonePosition();
                if (Vector2.Distance(player.position, newPosition) >= 6f)
                {
                    bossInstance.transform.position = newPosition;
                    break;
                }
            }

        }

        protected void UpdateIncreaseSpeed()
        {
            var multiplyBy = 1 + (increasePercentPerSecond * Time.deltaTime);
            foreach (var bullet in miniBombBullets)
            {
                bullet.BulletSpeed *= multiplyBy;
            }
        }

        protected GameObject miniProjectilePrefab;
        protected MirrorBossEncounter BD;
        protected Transform bossInstance;
        protected Transform player;

        protected float bombPlacePeriod = 0.3f;
        protected float bombPlaceTL = 0;
        protected int bombProjectileCount = 8;
        protected float projectileAdditionalSpeed = 0;
        protected float increasePercentPerSecond = 0.5f;
        protected ZoneScript teleportZone = null;
        protected List<EnemyBulletLife> miniBombBullets = null;
    }

    private class EllipseToCenterChaos : BossAttack
    {
        // Ellipse maths
        // We are using this formula for ellipse:
        //
        // x^2   y^2
        // --- + --- = (D/2)^2
        //  4     9
        //
        // This allows us to tweak D. The value of D means the right-most point of ellipse in Unity coordinates
        // So the starting D should be the right-most point of the arena
        // D lessens every frame so all projectiles move to the center of the room

        public EllipseToCenterChaos(BossEncounter bossData, float attackLength, bool allowInterruption = true, bool ended = false) : 
            base(bossData, attackLength, allowInterruption, ended)
        {
            BD = bossData as MirrorBossEncounter;
            ellipseProjectilePrefab = BD.ellipseBulletPrefab;
            upRightDistance = BD.distanceFromCenterToUpRight;
            roomCenter = BD.roomCenter.position;
        }

        public class EllipseBulletData
        {
            public Transform transform;
            float R;
            float xPos;
            bool rightSemisphere;
            bool topSemisphere;
            bool direction;

            public EllipseBulletData(Transform transform, float r, float xPos, bool leftSemisphere, bool topSemisphere, bool direction)
            {
                this.transform = transform;
                R = r;
                this.xPos = xPos;
                this.rightSemisphere = leftSemisphere;
                this.topSemisphere = topSemisphere;
                this.direction = direction;
            }

            public void MoveToCenter(Vector3 roomCenter, float diff)
            {
                R = R - diff;
                xPos = xPos - diff * (xPos / R); // decrease proportionally to maintain symmetry

                var yPos = Mathf.Sqrt(4 * (R * R - xPos * xPos) / 9);
                transform.position = roomCenter + new Vector3(rightSemisphere ? xPos : -xPos, topSemisphere ? yPos : -yPos, 0);
            }

            public void Orbit(float diff)
            {
                float semisphereSign = topSemisphere ? 1 : -1;
                semisphereSign *= rightSemisphere ? 1 : -1;
                float directionSign = direction ? 1 : -1;
                xPos = Mathf.Clamp(xPos + 0.5f * semisphereSign * directionSign * diff * R, 0, R - 0.001f);
                if (xPos >= R - 0.005f && rightSemisphere)
                {
                    topSemisphere = !direction;
                }
                else if (-xPos <= -R + 0.005f && !rightSemisphere)
                {
                    topSemisphere = direction;
                }
                else if (xPos <= 0.01f)
                {
                    rightSemisphere = topSemisphere;
                    if (!direction) rightSemisphere = !rightSemisphere;
                }
            }
        }

        protected override void AttackStart()
        {
            base.AttackStart();
            TestEllipse();
            Camera.main.GetComponent<CameraFocusOn>().FocusOn(roomCenter, attackLength, 4f);
        }

        protected override void AttackUpdate()
        {
            base.AttackUpdate();
            foreach (var bullet in ellipseBullets)
            {
                bullet.Orbit(Time.deltaTime);
                bullet.MoveToCenter(roomCenter, 8.75f * Time.deltaTime);
            }
        }

        protected override void AttackEnd()
        {
            base.AttackEnd();
            DirectBulletsOut();
            BD.mirrorBulletInfuser.infuseEnemyBullets = true;
            Camera.main.GetComponent<CameraFocusOn>().UnFocus(2, roomCenter);
        }

        private void TestEllipse()
        {
            var iCount = 30;
            for (float i = 0; i < iCount; i++)
            {
                ellipseBullets.Add(
                    SpawnBulletOnEllipseEdge(i / iCount * ellipseStartR, ellipseStartR, true, i % 2 == 1));
            }
            for (float i = 0; i < iCount; i++)
            {
                ellipseBullets.Add(
                    SpawnBulletOnEllipseEdge(i / iCount * ellipseStartR, ellipseStartR, false, i % 2 == 1));
            }
            //for (float i = 0; i < 17; i++)
            //{
            //    ellipseBullets.Add(
            //        SpawnBulletOnEllipseEdge(i / iCount * ellipseStartR * 0.5f, ellipseStartR / 2, true, i % 2 == 1));
            //}
        }

        private EllipseBulletData SpawnBulletOnEllipseEdge(float xPos, float R, bool leftSemisphere, bool topSemisphere)
        {
            R = R + Random.Range(rRange.x, rRange.y);
            var yPos = Mathf.Sqrt(9 * (R * R - xPos * xPos) / 4);
            Transform newBullet = Instantiate(ellipseProjectilePrefab, 
                roomCenter + new Vector3(leftSemisphere ? yPos : -yPos, topSemisphere ? xPos : -xPos, 0), Quaternion.identity).transform;
            return new EllipseBulletData(newBullet, R, xPos, leftSemisphere, topSemisphere, Random.Range(0, 2) == 1 ? true : false);
        }

        protected void DirectBulletsOut()
        {
            foreach (var bullet in ellipseBullets)
            {
                Vector3 toPlayer = bullet.transform.position - BD.player.position;
                float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

                var bulletComp = bullet.transform.GetComponent<EnemyBulletLife>();
                bulletComp.BulletSpeed = 15f;
            }

        }

        private GameObject ellipseProjectilePrefab;
        private MirrorBossEncounter BD;
        private Transform bossInstance;

        private Vector3 roomCenter;
        private Vector2 upRightDistance;
        private GameObject ellipseInstance;
        private Vector2 ellipse = new Vector2(9, 4);
        private Vector2 rRange = new Vector2(-0.5f, 4);
        private float ellipseStartR = 24;
        private Dictionary<Transform, float> bulletEllipseParameters = new Dictionary<Transform, float>();
        private Dictionary<Transform, bool> bulletEllipseSemisphere = new Dictionary<Transform, bool>();
        private List<EllipseBulletData> ellipseBullets = new List<EllipseBulletData>();
    }

    private class MirrorScreenEffect : BossAttack
    {
        public MirrorScreenEffect(BossEncounter bossData, float attackLength, float targetAlpha, bool allowInterruption = true, bool ended = false) 
            : base(bossData, attackLength, allowInterruption, ended)
        {
            MirrorBossEncounter BD = bossData as MirrorBossEncounter;
            glassEffect = BD.glassEffect;
            targetColor = glassEffect.color;
            targetColor.a = targetAlpha;
        }

        protected override void AttackStart()
        {
            base.AttackStart();
            startingColor = glassEffect.color;
        }

        protected override void AttackUpdate()
        {
            glassEffect.color = Color.Lerp(targetColor, startingColor, attackTimeLeft / attackLength);
        }

        private SpriteRenderer glassEffect = null;
        private Color startingColor;
        private Color targetColor;
    }

    public class InitialPhase : BossPhase
    {
        public InitialPhase(BossEncounter bossData) : base(bossData)
        {
            phaseName = "Initial phase";
            phaseLength = 1.5f;
            phaseType = PhaseType.TimeBased;
            attackOrder = AttackOrder.Sequence;
            attacks = new List<BossAttack>()
            {
                new SpawnBossAttack(bossData, phaseLength)
            };
        }
    }

    public class AvoidancePhase : BossPhase
    {
        public AvoidancePhase(BossEncounter bossData) : base(bossData)
        {
            phaseName = "Avoidance";
            phaseLength = 19f;
            phaseType = PhaseType.AttackBased;
            attackOrder = AttackOrder.Sequence;
            attacks = new List<BossAttack>()
            {
                new ExplosionAttack(bossData, 2.3f, 9, waitBefore: 0.9f),
                new ExplosionAttack(bossData, 1.7f, 9),
                new ExplosionAttack(bossData, 1.4f, 9, returnBack: false),
                new MultibombAttack(bossData, 2.15f, additionalSpeed: 4f),  // 6 ticks
                new ExplosionAttack(bossData, 1.4f, 9),
                new ExplosionAttack(bossData, 1.7f, 9),
                new ExplosionAttack(bossData, 1.3f, 9, returnBack: false),
                new MultibombAttack(bossData, 3.2f, additionalSpeed: 1.5f),   // 9 ticks + end
                new EllipseToCenterChaos(bossData, 2.5f),
                new MirrorScreenEffect(bossData, 0.8f, 0.5f), // Change with mirror turn-on
            };
        }

        protected override void PhaseUpdate()
        {
            base.PhaseUpdate();
        }
    }

    public class MirrorSpawnEnemy : BossAttack
    {
        public MirrorSpawnEnemy(BossEncounter bossData, float attackLength, GameObject enemyToSpawn, bool allowInterruption = true, bool ended = false) 
            : base(bossData, attackLength, allowInterruption, ended)
        {
            BD = bossData as MirrorBossEncounter;
            spawnZones = BD.spawnZones;
            this.enemyToSpawn = enemyToSpawn;
        }

        protected override void AttackStart()
        {
            base.AttackStart();
            int zoneIndex = Random.Range(0, spawnZones.Length);
            Vector2 randomPosition = spawnZones[zoneIndex].RandomZonePosition();
            var monster = Instantiate(enemyToSpawn, randomPosition, Quaternion.identity);
            var monsterAttack = monster.GetComponent<Attack>();
            if (monsterAttack) monsterAttack.ForceAttack();
            monster.GetComponent<MonsterDrop>().anyDropChance = 0;
            BD.spawnedMonsters.Add(monster.GetComponent<MonsterLife>());
            foreach (var obj in monster.GetComponentsInChildren<SpriteRenderer>())
            {
                if (obj.gameObject.name.StartsWith("Eye"))
                {
                    obj.color = BD.mirrorColor;
                }
            }
        }

        GameObject enemyToSpawn = null;
        MirrorBossEncounter BD = null;
        ZoneScript[] spawnZones;
    }

    public class BreakMirror : BossAttack
    {
        public BreakMirror(BossEncounter bossData, float attackLength, bool allowInterruption = true, bool ended = false) : 
            base(bossData, attackLength, allowInterruption, ended)
        {
            BD = bossData as MirrorBossEncounter;
        }

        protected override void AttackEnd()
        {
            base.AttackEnd();
            DestroyEveryone();
            BD.glassShards.SetActive(true);
            BD.mirrorCracks.SetActive(true);
        }

        public void DestroyEveryone()
        {
            foreach (var monster in BD.spawnedMonsters)
            {
                monster.Damage(null, 99999, true);
            }
        }

        MirrorBossEncounter BD;
    }

    public class MirrorPhase : BossPhase
    {
        public MirrorPhase(BossEncounter bossData) : base(bossData)
        {
            phaseName = "MirrorPhase";
            phaseLength = 15.2f;
            phaseType = PhaseType.TimeBased;
            attackOrder = AttackOrder.Sequence;
            BD = bossData as MirrorBossEncounter;
            attacks = new List<BossAttack>()
            {
                new MirrorSpawnEnemy(bossData, 1, BD.zombiePrefab),
                new MirrorSpawnEnemy(bossData, 1, BD.shootingMonsterPrefab),
                new MirrorSpawnEnemy(bossData, 1, BD.teleportMonsterPrefab),
                new MirrorSpawnEnemy(bossData, 1, BD.zombiePrefab),
                new MirrorSpawnEnemy(bossData, 1, BD.ghostPrefab),
                new MirrorSpawnEnemy(bossData, 1, BD.tankPrefab),
                new MirrorSpawnEnemy(bossData, 1, BD.chaosMonsterPrefab), // 7
                new MirrorSpawnEnemy(bossData, 1, BD.ghostPrefab),
                new MirrorSpawnEnemy(bossData, 1, BD.teleportMonsterPrefab),
                new MirrorSpawnEnemy(bossData, 1, BD.tankPrefab),
                new MirrorSpawnEnemy(bossData, 1, BD.zombiePrefab),
                new MirrorSpawnEnemy(bossData, 0.5f, BD.shootingMonsterPrefab),
                new MirrorSpawnEnemy(bossData, 0.5f, BD.chaosMonsterPrefab),
                new MirrorSpawnEnemy(bossData, 0.5f, BD.shootingMonsterPrefab),
                new MirrorSpawnEnemy(bossData, 0.1f, BD.tankPrefab),
                new MirrorSpawnEnemy(bossData, 0.1f, BD.shootingMonsterPrefab),
                new MirrorSpawnEnemy(bossData, 0.1f, BD.tankPrefab),
                new MirrorSpawnEnemy(bossData, 0.1f, BD.shootingMonsterPrefab),
                new MirrorSpawnEnemy(bossData, 0.75f, BD.tankPrefab),  // perfect
                new MirrorScreenEffect(bossData, 0.85f, 1),
                new BreakMirror(bossData, 0.2f),
                new MirrorScreenEffect(bossData, 0.2f, 0),
                new BossAttack(bossData, 100) // increase chromatic abberation
            };
            foreach (var spawnZone in BD.spawnZones)
            {
                spawnZone.UseZone();
            }
        }

        protected override void OnNextAttackStart()
        {
            base.OnNextAttackStart();
        }

        public override void StartPhase()
        {
            base.StartPhase();
            GameObject.FindGameObjectWithTag("GameController").GetComponent<CurrentEnemySelector>().enableScanning = true;
            BD.GetComponentInChildren<ContiniousOutlineAppear>().Activate();
            if (BD.bossInstance) // in debug mode there is no boss
            {
                BD.bossInstance.gameObject.SetActive(false);
            }
        }

        public override void DebugStartPhase()
        {
            AudioManager.PlayMusic(BD.GetComponent<AudioSource>(), 18.5f);
            base.DebugStartPhase();
        }

        private MirrorBossEncounter BD;
    }

    public class MirrorOutrage : BossAttack
    {
        public MirrorOutrage(MirrorBossEncounter bossData, float attackLength, bool allowInterruption = true, bool ended = false) : base(bossData, attackLength, allowInterruption, ended)
        {
            BD = bossData;
            movePosition = BD.movePosition;
            movePosition.UseZone();
            projectile = BD.outrageBullet;
        }

        protected override void AttackStart()
        {
            base.AttackStart();
            moveTo = movePosition.RandomZonePosition();
            moveFrom = BD.bossInstance.position;
            float distanceToMove = Vector3.Distance(moveFrom, moveTo);
            moveTime = Mathf.Lerp(minMoveTime, maxPossibleMoveTime, 
                (Mathf.Clamp(distanceToMove, minnDTimeFactor, maxDTimeFactor) - minnDTimeFactor) / (maxDTimeFactor - minnDTimeFactor));
            moveTimeLeft = moveTime;
            attackTimeLeft += moveTime;
        }

        protected override void AttackUpdate()
        {
            if (!BD.bossInstance) return;
            base.AttackUpdate();
            if (moveTimeLeft >= 0) Move();
            else if (timeToNextShot <= 0) Attack();
            else timeToNextShot -= Time.deltaTime;
        }

        protected void Move()
        {
            BD.bossInstance.position = Vector3.Lerp(moveTo, moveFrom, moveTimeLeft / moveTime);
            moveTimeLeft -= Time.deltaTime;
        }

        protected void Attack()
        {
            var newBullet = Instantiate(projectile, BD.bossInstance.position, Quaternion.Euler(0, 0, BD.bossInstance.rotation.eulerAngles.z + 90 + Random.Range(-10f, 10f)));
            newBullet.GetComponent<EnemyBulletLife>().ignoreCollisionTime = 10f;
            newBullet.GetComponentInChildren<SpriteRenderer>().color = BD.mirrorColor;

            var newBullet2 = Instantiate(projectile, BD.bossInstance.position, 
                Quaternion.Euler(0, 0, BD.bossInstance.rotation.eulerAngles.z + 90 + (Random.Range(0, 2) == 0 ? 1 : -1) * Random.Range(20f, 30f)));
            newBullet2.GetComponent<EnemyBulletLife>().ignoreCollisionTime = 10f;
            newBullet2.GetComponentInChildren<SpriteRenderer>().color = BD.mirrorColor;

            timeToNextShot = Random.Range(timeToShot.x, timeToShot.y);
        }

        MirrorBossEncounter BD;
        ZoneScript movePosition;
        GameObject projectile;
        private float maxDTimeFactor = 5f;
        private float minnDTimeFactor = 1f;
        private float timeToNextShot = 0;
        private float minMoveTime = 0.2f;
        private float maxPossibleMoveTime = 1f;
        private float moveTime;
        private float moveTimeLeft;
        private Vector3 moveFrom;
        private Vector3 moveTo;
        private Vector2 timeToShot = new Vector2(0.15f, 0.5f);
    }

    public class ConfrontPhase : BossPhase
    {
        public ConfrontPhase(BossEncounter bossData) : base(bossData)
        {
            phaseName = "Confront";
            phaseLength = 100f;
            phaseType = PhaseType.HpBased;
            attackOrder = AttackOrder.RandomRepeatable;
            endHpPercentage = 0;
            BD = bossData as MirrorBossEncounter;
            attacks = new List<BossAttack>()
            {
                new MirrorOutrage(BD, 2f)
            };
        }

        public override void StartPhase()
        {
            base.StartPhase();
            BD.bossInstance.gameObject.SetActive(true);
            BD.bossHP = BD.bossInstance.GetComponent<MonsterLife>();
            BD.bossInstance.GetComponent<MonsterLife>().SetMinHpPercentage(0);
            BD.bossInstance.GetComponent<Face>().target = BD.mirrorCracks;
            BD.bossInstance.GetComponent<Chapter1BossMonsterLife>().hitNonMirror = false;
            foreach (var obj in BD.bossInstance.GetComponentsInChildren<SpriteRenderer>())
            {
                if (obj.gameObject.name.StartsWith("Eye"))
                {
                    obj.color = BD.mirrorColor;
                }
            }
        }

        protected override void EndPhase()
        {
            base.EndPhase();
            BD.StartCoroutine(BD.EndGame());
        }

        public override void DebugStartPhase()
        {
            base.DebugStartPhase();
            AudioManager.PlayMusic(BD.GetComponent<AudioSource>(), 33f);
        }

        MirrorBossEncounter BD;
    }

    protected override void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        bossPhases = new List<BossPhase>()
        {
            new InitialPhase(this),
            new AvoidancePhase(this),
            new MirrorPhase(this),
            new ConfrontPhase(this),
        };
        
        //Camera.main.GetComponent<CameraFocusOn>().FocusOn(player.position, 3f, 2f);
    }

    protected override void Update()
    {
        if (encounterOver)
        {
            AudioManager.SetVolumeMusic(AudioManager.userPrefMusic - 0.2f - (Time.deltaTime * 0.1f)); // Гребаный костыль вали его Валера
        }
        base.Update();
    }

    protected override void EncounterUpdate()
    {
        encounterOver = encounterOver || CharacterLife.isDeath;
        base.EncounterUpdate();
    }

    public void StartFight()
    {
        encounterStarted = true;
        base.Start();
    }

    public IEnumerator EndGame()
    {
        yield return new WaitForSeconds(3f);
        Metrics.OnWin();
        RelodScene.OnSceneChange?.Invoke();
        SceneManager.LoadScene("Scoreboard");
    }
}