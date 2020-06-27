﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    [SerializeField] private float timeToEachSpawn = 5;
    [SerializeField] private float timeToNextSpawn = 0;
    [SerializeField] protected GameObject[] enemyWaves = null;
    [SerializeField] protected List<ZoneScript> spawnZones = new List<ZoneScript>();
    public int killsToOpen = 999; // if more than monsters in the room, iot will open when all is dead

    [HideInInspector] public Vector2 RoomBounds = new Vector2(15, 10);
    [HideInInspector] public bool spawnAvailable = false;
    [HideInInspector] public RoomLighting roomLighting;
    [HideInInspector] public List<GameObject> strayMonsters;
    [HideInInspector] public List<GameObject> monsterList;
    [HideInInspector] public Room room;

    public List<MonsterRoomModifier> monsterRoomModifiers = new List<MonsterRoomModifier>();

    [SerializeField]
    protected bool AllowEarlySpawns = true;
    protected int spawnIndex = 0;
    private int killCount = 0;

    private void Awake()
    {
        if (room == null) // to prevent double init
            Init();
    }

    public void Init()
    {
        roomLighting = GetComponent<RoomLighting>();
        strayMonsters = new List<GameObject>();
        if (GetComponent<Room>() != null)
        {
            room = GetComponent<Room>();
            room.monsterManager = this;
            room.externalMRMods.ForEach(mod => monsterRoomModifiers.Add(mod));
        }
        else
            Debug.LogError("MonsterManager can't find room script");

        foreach (var spawnZone in spawnZones)
        {
            spawnZone.UseZone();
        }

        foreach (GameObject monster in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if (monster.transform.IsChildOf(transform))
            {
                strayMonsters.Add(monster);
                monsterList.Add(monster);
                monster.SetActive(false);
                var monsterLife = monster.GetComponent<MonsterLife>();
                monsterRoomModifiers.ForEach(mod => mod.ApplyModifier(monsterLife));
                monsterLife.monsterManager = this;
            }
            else
            {
                print("If this piece of code triggers, then the condition above is truly necessary");
            }
        }
        killCount = 0;
    }

    private Vector2 RandomBorderSpawnPos()
    {
        var spawnPosition = new Vector2();
        var dice = Random.Range(0, 4);
        // North, South, East, West spawn positions
        switch (dice)
        {
            case 0:
                spawnPosition.y = RoomBounds.y;
                spawnPosition.x = Random.Range(-RoomBounds.x, RoomBounds.x);
                break;
            case 1:
                spawnPosition.y = -RoomBounds.y;
                spawnPosition.x = Random.Range(-RoomBounds.x, RoomBounds.x);
                break;
            case 2:
                spawnPosition.x = RoomBounds.x;
                spawnPosition.y = Random.Range(-RoomBounds.y, RoomBounds.y);
                break;
            case 3:
                spawnPosition.x = -RoomBounds.x;
                spawnPosition.y = Random.Range(-RoomBounds.y, RoomBounds.y);
                break;
        }
        spawnPosition += (Vector2)gameObject.transform.position; // shift to room position
        return spawnPosition;
    }

    protected void SetMonsterPosition(GameObject enemy)
    {
        if (spawnZones.Count != 0)
        {
            enemy.transform.position = spawnZones[Random.Range(0, spawnZones.Count)].RandomZonePosition();
        }
        else
        {
            enemy.transform.position = RandomBorderSpawnPos();
        }
    }
    
    private void SpawnMonsters(int waveNum)
    {
        var enemyWave = Instantiate(enemyWaves[waveNum], transform.position, Quaternion.identity);
        enemyWave.transform.parent = room.transform; //to delete extra mobs with room

        int enemiesInWave = enemyWave.transform.childCount; 

        for (int i = 0; i < enemiesInWave; i++)
        {
            GameObject enemy = enemyWave.transform.GetChild(i).gameObject;
            var behaviours = enemy.GetComponentsInChildren<EnemyBehavior>();
            // Make enemies move towards player always
            foreach (var behaviour in behaviours)
            {
                if (!(behaviour is Attack))
                {
                    behaviour.Activate();
                    behaviour.timeToLoseAggro = -1; // never stop moving
                }
            }

            monsterList.Add(enemy);
            var monsterLife = enemy.GetComponent<MonsterLife>();
            monsterLife.monsterManager = this;
            monsterRoomModifiers.ForEach(mod => mod.ApplyModifier(monsterLife));

            SetMonsterPosition(enemy);
        }
    }

    public void Death(GameObject monster) {        
        roomLighting.LabirintRoomAddLight();
        monsterList.Remove(monster);
        if (strayMonsters.Contains(monster))
            strayMonsters.Remove(monster);
        killCount++;
        WinCheck();
    }

    void WinCheck() {
        if ((monsterList.Count == 0 && spawnIndex == enemyWaves.GetLength(0)) || (killCount>=killsToOpen)) {
            room.UnlockRoom();
            while (monsterList.Count>0)
            {
                if (monsterList[0].GetComponent<MonsterDrop>()!=null)
                    monsterList[0].GetComponent<MonsterDrop>().enabled = false;
                if (monsterList[0].GetComponent<SpawnOnDeath>() != null)
                    monsterList[0].GetComponent<SpawnOnDeath>().spawnBlock = true;
                monsterList[0].GetComponent<MonsterLife>().Damage(gameObject, 9999f, true);
            }
            spawnAvailable = false;
        }
    }

    protected virtual void Update()
    {
        if (Pause.Paused) return;
        if (spawnAvailable)
            EnemySpawnUpdate();
        if (room.roomID == Labirint.instance.currentRoomID)// only for current room
        {
            if (CharacterLife.isDeath)
            {
                GameObject pressFGUI = GameObject.FindGameObjectWithTag("Canvas").transform.GetChild(1).gameObject;
                if (!pressFGUI.activeSelf)
                    pressFGUI.SetActive(true);
            }
        }
    }

    public void KillThemAll()
    {
        foreach (GameObject monster in monsterList)
            monster.GetComponent<MonsterLife>().Damage(null, 999, ignoreInvulurability: true);
        monsterList = new List<GameObject>();
        strayMonsters = new List<GameObject>();
    }

    protected void EnemySpawnUpdate()
    {
        timeToNextSpawn -= Time.deltaTime;
        if ((timeToNextSpawn < 0 || (monsterList.Count == 0 && AllowEarlySpawns)) && spawnIndex < enemyWaves.GetLength(0))
        {
            timeToNextSpawn = timeToEachSpawn;
            SpawnMonsters(spawnIndex);
            spawnIndex++;
        }
    }

    public int EnemyCount()
    {
        int enemiesCount = 0;
        foreach (var e in enemyWaves)
        {
            enemiesCount += e.transform.childCount;
        }
        enemiesCount += strayMonsters.Count;
        return enemiesCount;
    }

    public void UnfreezeMonsters() {
        spawnAvailable = true;
        foreach (GameObject monster in strayMonsters) {
            monster.SetActive(true);
        }
    }
}
