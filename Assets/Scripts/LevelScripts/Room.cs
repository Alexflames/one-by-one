﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    private Door[] doors;
    public Dictionary<Direction.Side, Door> doorsSided = new Dictionary<Direction.Side, Door>();

    [HideInInspector]
    private Labirint labirint = null;
    public int roomID = -1; // -1 for not set

    public enum RoomType {empty,arena }
    public RoomType roomType;

    public Transform possibleContainerPosition;
    public bool containerAlreadySpawned = false;

    [HideInInspector] public MonsterManager monsterManager;
    [HideInInspector] public List<MonsterRoomModifier> externalMRMods = new List<MonsterRoomModifier>();

    private void Awake()
    {
        labirint = Labirint.instance;
        externalMRMods = labirint.commonMRMods;
        if (possibleContainerPosition == null) possibleContainerPosition = transform; // if forgot to set, center of room
    }

    private void Start()
    {
        DoorsInit();
        FillOOB();
    }

    public void DoorsInit() {
        doors = gameObject.GetComponentsInChildren<Door>();
        foreach (Door door in doors)
        {
            if (door.sceneName == "") {
                if (door.direction == Direction.Side.UNSET && door.directionAutoset())
                     Debug.LogError("Door direction was not set");
                else doorsSided[door.direction] = door;
                if (door.room == null) door.room = this;
            }
        }
    }

    public void MoveToRoom(Door wayInDoor) {
        wayInDoor.connectedDoor.room.LeaveRoom();
        CameraForLabirint.instance.ChangeRoom(wayInDoor.room.gameObject);
        GameObject.FindGameObjectWithTag("Player").transform.position = wayInDoor.transform.position;
        Labirint.instance.OnRoomChanged(roomID);
        ArenaInitCheck();
        LightCheck();
    }

    public void ArenaInitCheck()
    {
        if (roomType == RoomType.arena)
        {            
            if (!Labirint.instance.blueprints[roomID].visited) 
            {
                if(GetComponent<ArenaEnemySpawner>()!=null)
                    GetComponent<ArenaEnemySpawner>().enabled = true;
                if (GetComponent<MonsterManager>() != null)
                    GetComponent<MonsterManager>().UnfreezeMonsters();
                LockRoom();
            }
            else {
                if (GetComponent<ArenaEnemySpawner>() != null)
                    GetComponent<ArenaEnemySpawner>().KillThemAll();
                TimerUnlockRoom();
            }
        }
        else
        {
            TimerUnlockRoom();
        }
    }

    public void DisconnectRoom() // cutting door connections before destroy room, to avoid errors on Door.connectedDoor from neighors to destroyed room
    {
        foreach (Door door in doors) {
            if (door.connectedDoor != null) {
                door.connectedDoor.connectedDoor = null;
                door.connectedDoor = null;
            }
        }
    }

    public void UnlockRoom(){
        foreach (Door door in doors) {
            door.Unlock();
        }
        CameraForLabirint.instance.CameraFreeSetup();
    }

    public void LockRoom() {
        foreach (Door door in doors)
        {
            door.Lock();
        }
    }

    public void TimerUnlockRoom() {
        foreach (Door door in doors)
        {
            door.unlockOnTimer = true;
            door.Lock();
        }
    }

    public void LeaveRoom() {
        if (roomType == RoomType.arena)
        {
            GetComponent<ArenaEnemySpawner>()?.KillThemAll();
        }
        Labirint.instance.blueprints[roomID].visited = true;        
    }

    public void LightCheck() {
        if (monsterManager != null)
            if (roomType == RoomType.arena && !labirint.blueprints[roomID].visited)
                monsterManager.roomLighting.LabirintRoomEnterDark(monsterManager.EnemyCount());
            else
                monsterManager.roomLighting.LabirintRoomEnterBright();
        else
            GetComponent<RoomLighting>().LabirintRoomEnterBright(); // exception for room without monsters

    }
    
    public Dictionary<Direction.Side, float> GetBordersFromTilemap() {
        Dictionary<Direction.Side, float> result = new Dictionary<Direction.Side, float>();
        Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>();
        float left, right, up, down;
        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap.tag == "Environment")
            { // to separete layer with walls
                Vector3Int tilePosition;
                left = Mathf.Infinity;
                right = -Mathf.Infinity;
                up = -Mathf.Infinity;
                down = Mathf.Infinity;
                for (int x = tilemap.origin.x; x < tilemap.size.x; x++)
                {
                    for (int y = tilemap.origin.y; y < tilemap.size.y; y++)
                    {
                        tilePosition = new Vector3Int(x, y, 0);
                        if (tilemap.HasTile(tilePosition))
                        {
                            if (tilemap.CellToWorld(tilePosition).x < left) left = tilemap.CellToWorld(tilePosition).x;
                            if (tilemap.CellToWorld(tilePosition).x > right) right = tilemap.CellToWorld(tilePosition).x;
                            if (tilemap.CellToWorld(tilePosition).y < down) down = tilemap.CellToWorld(tilePosition).y;
                            if (tilemap.CellToWorld(tilePosition).y > up) up = tilemap.CellToWorld(tilePosition).y;
                        }
                    }
                }
                result[Direction.Side.LEFT] = left + 1.5f; //mb need to replace with something tile size related later
                result[Direction.Side.RIGHT] = right + 0.5f;
                result[Direction.Side.UP] = up + 0.5f;
                result[Direction.Side.DOWN] = down + 1.5f;                
            }
        }
        return result;
    }

    public bool RectIsInbounds(float x, float y, float sizeX, float sizeY) {
        bool result = true;
        Dictionary<Direction.Side, float> bounds = GetBordersFromTilemap();
        result = x > bounds[Direction.Side.LEFT] &&
            x + sizeX < bounds[Direction.Side.RIGHT] &&
            y > bounds[Direction.Side.DOWN] &&
            y + sizeY < bounds[Direction.Side.UP];
        return result;
    }

    private Tilemap walls = null;
    private int brakeCounter = 0;
    private Vector3Int inboundsPosituion;
    private int[,] map; // 0 - dont know, 1 - oob, 2 - inbounds, 3 - border
    private int topBorder,botBorder,leftBorder,rightBorder;

    private void FillOOB()
    {
        GetWallTilemap();
        Dictionary<Direction.Side, float> borders = GetBordersFromTilemap();
        topBorder = walls.WorldToCell(new Vector3(0, borders[Direction.Side.UP], 0)).y + 3;
        botBorder = walls.WorldToCell(new Vector3(0, borders[Direction.Side.DOWN], 0)).y - 3;
        leftBorder = walls.WorldToCell(new Vector3(borders[Direction.Side.LEFT], 0, 0)).x - 3;
        rightBorder = walls.WorldToCell(new Vector3(borders[Direction.Side.RIGHT], 0, 0)).x + 3;

        map = new int[rightBorder - leftBorder + 1, topBorder - botBorder + 1];
        FillOuterCell2(0, 0);
        brakeCounter = 0;
        if (GetInboundsPoint()) // if can find inside point
        {
            brakeCounter = 0;
            FillInboundsCell2(inboundsPosituion.x-leftBorder, inboundsPosituion.y-botBorder);
            FillRest();
            PaintBorder();
        }
        else {
            map = null; //to prevent telepot in case of crash
        }
        //DrawDebug();
    }

    private void GetWallTilemap()
    { // get walls tilemap layer and set it to var walls
        if (walls == null)
        {
            Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>();
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap.tag == "Environment")
                    walls = tilemap;
            }
        }
    }

    //    private void FillOuterCell(int x, int y)
    //    {
    //        OuterDirectionCheck(x, y, x + 1, y);
    //        OuterDirectionCheck(x, y, x - 1, y);
    //        OuterDirectionCheck(x, y, x, y + 1);
    //        OuterDirectionCheck(x, y, x, y - 1);
    //    }    

    //private void OuterDirectionCheck(int oldx, int oldy, int newx, int newy)
    //{
    //    if (newx >= 0 && newy >= 0 &&
    //        newx <= rightBorder-leftBorder && newy <= topBorder-botBorder)
    //    {
    //        if (map[newx, newy] == 0) 
    //        {
    //            if ( !(walls.HasTile(new Vector3Int(oldx + leftBorder, oldy+ botBorder, 0)) && !walls.HasTile(new Vector3Int(newx+ leftBorder, newy+ botBorder, 0))))
    //            {// except transition form filled to not filled tile
    //                map[newx, newy] = 1;
    //                brakeCounter++;
    //                //Debug.Log(newx.ToString() + " " + newy.ToString());
    //                if (brakeCounter < 2000)
    //                {
    //                    FillOuterCell(newx, newy);
    //                }
    //            }
    //        }
    //    }        
    //}

    private void FillOuterCell2(int x, int y) {
        List<Vector3Int> freshCells = new List<Vector3Int>();
        freshCells.Add(new Vector3Int(x, y, 0));
        List<Vector3Int> nextGenegationCells;
        List<Vector3Int> posibleShifts = new List<Vector3Int> { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
        Vector3Int arrayToTilemap = new Vector3Int(leftBorder, botBorder, 0);
        Vector3Int newCell;
        while (freshCells.Count > 0) {
            nextGenegationCells = new List<Vector3Int>();
            foreach (Vector3Int oldCell in freshCells) {
                foreach (Vector3Int shift in posibleShifts)
                {
                    brakeCounter++;
                    {
                        newCell = oldCell + shift;
                        if (newCell.x >= 0 && newCell.y >= 0 &&
                        newCell.x <= rightBorder-leftBorder && newCell.y <= topBorder-botBorder)
                        {
                            if ((map[newCell.x, newCell.y] == 0) && !(walls.HasTile(oldCell + arrayToTilemap) && !walls.HasTile(newCell + arrayToTilemap)))
                            {
                                nextGenegationCells.Add(newCell);
                                map[newCell.x, newCell.y] = 1;
                            }
                        }
                    }
                }
            }
            freshCells = new List<Vector3Int>(nextGenegationCells);
        }
    }

    private bool GetInboundsPoint() { //эвристика, чертим линию от левой двери вправо пока не найдем пустую клетку.
        bool found = false;
        Vector3Int currentPos = walls.WorldToCell(doorsSided[Direction.Side.LEFT].transform.position);
        while (!found && (currentPos.x<rightBorder)&& brakeCounter<100) {
            //Debug.Log(currentPos.x.ToString()+" "+ rightBorder.ToString());
            brakeCounter++;
            if (!walls.HasTile(currentPos)) {
                found = true;
                inboundsPosituion = currentPos;
                return true;
            }
            else
                currentPos += Vector3Int.right;            
        }
    
        if (!found) {
            Debug.Log("Cant find inbounds");
            return false;
        }else return true;
    }

    //private void FillInboundsCell(int x, int y) {
    //    InboundsDirectionCheck(x, y, x + 1, y);
    //    InboundsDirectionCheck(x, y, x - 1, y);
    //    InboundsDirectionCheck(x, y, x, y + 1);
    //    InboundsDirectionCheck(x, y, x, y - 1);
    //}
    //
    //private void InboundsDirectionCheck(int oldx, int oldy, int newx, int newy)
    //{
    //    if (newx >= 0 && newy >= 0 &&
    //        newx <= rightBorder - leftBorder && newy <= topBorder - botBorder)
    //    {
    //        if (map[newx, newy] == 0)
    //        {
    //            if (!walls.HasTile(new Vector3Int(newx + leftBorder, newy + botBorder, 0)))
    //            {
    //                map[newx, newy] = 2;
    //                brakeCounter++;
    //                if (brakeCounter < 3000)
    //                {
    //                    FillInboundsCell(newx, newy);
    //                }
    //            }
    //        }
    //    }
    //}

    private void FillInboundsCell2(int x, int y) {
        List<Vector3Int> freshCells = new List<Vector3Int>();
        freshCells.Add(new Vector3Int(x, y, 0));
        List<Vector3Int> nextGenegationCells;
        List<Vector3Int> posibleShifts = new List<Vector3Int> { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
        Vector3Int arrayToTilemap = new Vector3Int(leftBorder, botBorder, 0);
        Vector3Int newCell;
        while (freshCells.Count > 0)
        {
            brakeCounter++;
            if (brakeCounter < 10000)
            {
                nextGenegationCells = new List<Vector3Int>();
                foreach (Vector3Int oldCell in freshCells)
                {
                    foreach (Vector3Int shift in posibleShifts)
                    {
                        {
                            newCell = oldCell + shift;
                            if (newCell.x >= 0 && newCell.y >= 0 &&
                            newCell.x <= rightBorder - leftBorder && newCell.y <= topBorder - botBorder)
                            {
                                if ((map[newCell.x, newCell.y] == 0) && !walls.HasTile(newCell + arrayToTilemap))
                                {
                                    nextGenegationCells.Add(newCell);
                                    map[newCell.x, newCell.y] = 2;
                                }
                            }
                        }
                    }
                }
                freshCells = new List<Vector3Int>(nextGenegationCells);
            }
        }
    }

    private void FillRest() {
        for (int x = 0; x < rightBorder - leftBorder - 1; x++)
        {
            for (int y = 0; y < topBorder - botBorder - 1; y++)
            {
                if (map[x, y] == 0)
                    map[x, y] = 1;
            }
        }
    }

    private void PaintBorder() {
        bool isborder;
        for (int x = 1; x < rightBorder - leftBorder - 1; x++)
        {
            for (int y = 1; y < topBorder - botBorder - 1; y++)
            {
                if (map[x, y] == 1) {
                    isborder = false;
                    isborder = isborder || (map[x + 1,  y]      == 2); // if has inbounds cell in any of 9 dirrections
                    isborder = isborder || (map[x + 1,  y + 1]  == 2);
                    isborder = isborder || (map[x,      y + 1]  == 2);
                    isborder = isborder || (map[x - 1,  y + 1]  == 2);
                    isborder = isborder || (map[x - 1,  y]      == 2);
                    isborder = isborder || (map[x - 1,  y - 1]  == 2);
                    isborder = isborder || (map[x,      y - 1]  == 2);
                    isborder = isborder || (map[x + 1,  y - 1]  == 2);
                    if (isborder) map[x, y] = 3;
                }                
            }
        }
    }

    public bool PositionIsInbounds(Vector3 position) {
        bool result = true;
        if (map != null && walls !=null) {
            Vector3Int positionOnTilemap = walls.WorldToCell(position);
            if (positionOnTilemap.x<leftBorder || positionOnTilemap.x>rightBorder ||
                positionOnTilemap.y>topBorder || positionOnTilemap.y<botBorder) {
                result = false;
            }
            else if (map[positionOnTilemap.x - leftBorder, positionOnTilemap.y - botBorder] == 1)
                result = false;
        }
        return result;
    }

    public Vector3 GetNearInboundsPosition(Vector3 currentPosition) { // осторожно хардкод. Нервным не смотреть
        Vector3 result = currentPosition;
        float shiftAmp = 1;
        Vector3[] possibleShifts = new Vector3[8] { Vector3.up, Vector3.up + Vector3.right, Vector3.right, Vector3.right + Vector3.down, Vector3.down, Vector3.down + Vector3.left, Vector3.left, Vector3.left + Vector3.up };
        for (int i = 1; i < 20; i++)
        {
            shiftAmp = i;
            foreach (Vector3 shift in possibleShifts)
            {
                if (PositionIsInbounds(currentPosition + (shift * shiftAmp)))
                    return currentPosition + (shift * shiftAmp);
            }
        }
        Debug.Log("Cant find inbounds position");
        return result;
    }

    private void DrawDebug()
    {
        for (int x = leftBorder; x < rightBorder - 1; x++)
            for (int y = botBorder; y < topBorder - 1; y++)
            {
                if (map[x - leftBorder, y - botBorder] == 1)
                    Debug.DrawRay(walls.CellToWorld(new Vector3Int(x, y, 0)), Vector3.up, Color.red, 99f);
                if (map[x - leftBorder, y - botBorder] == 2)
                    Debug.DrawRay(walls.CellToWorld(new Vector3Int(x, y, 0)), Vector3.up, Color.green, 99f);                
                if (map[x - leftBorder, y - botBorder] == 3)
                    Debug.DrawRay(walls.CellToWorld(new Vector3Int(x, y, 0)), Vector3.up, Color.yellow, 99f);
            }
    }
}
