﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LabirintBuilder : MonoBehaviour
{
    [SerializeField]
    private int numberOfRooms = 10;
    [SerializeField]
    private int correctPathLength = 3;
    [SerializeField]
    private GameObject[] combatRoomPrefabs = null;
    [SerializeField]
    private GameObject[] peacefulRoomPrefabs = null;
    [SerializeField]
    private string exitSceneName = "";
    [SerializeField]
    private GameObject[] containersPrefabs = null;
    [SerializeField]
    private bool roomRepeatAllowed = false;
    [SerializeField]
    private int treasureRoomsNumber = 0;
    [SerializeField]
    private GameObject[] treasureRoomPrefabs = null;

    private Labirint labirint;
    private int[,] map; //room position to room id
    private Dictionary<int, Vector2Int> allRoomsPositions; // room id to room position
    private List<Vector2Int> correctPathRoomsPositions;
    private Vector2Int startPosition;
    private Vector2Int endPosition;
    private Vector2Int currentPosition;
    private int lastRoomID;

    static public string seed = "";

    private void Init()
    {
        map = new int[2*numberOfRooms, 2*numberOfRooms];
        for (int i = 0; i < 2 * numberOfRooms; i++)
            for (int j = 0; j < 2 * numberOfRooms; j++) {
                map[i, j] = -1;
            }
    }

    public void BuildLabirint(Labirint labirintScript) {
        labirint = labirintScript;
        Init();
        labirint.blueprints = new RoomBlueprint[numberOfRooms];
        labirint.InitBlueprintsFromBuilder();
        startPosition = new Vector2Int(numberOfRooms, numberOfRooms); // середина
        currentPosition = startPosition;
        allRoomsPositions = new Dictionary<int, Vector2Int>();
        allRoomsPositions.Add(0,startPosition);
        correctPathRoomsPositions = new List<Vector2Int>();
        correctPathRoomsPositions.Add(startPosition);
        map[startPosition.x, startPosition.y] = 0;
        lastRoomID = 0;

        SeedCheck();

        MakeCorrectPath();
        MakeDeadEnds();
        FillRoomPrefabs();
        //FillContainers();
        FillTreasureRooms();
        //DrawMap();
    }

    void MakeCorrectPath() {
        Vector2Int newPosition;
        Vector2Int positionToMove;
        List<Direction.Side> availableSides;
        while (lastRoomID < correctPathLength-1)
        {
            availableSides = new List<Direction.Side>();
            foreach (Direction.Side side in Direction.sides)
            {
                positionToMove = currentPosition + Direction.SideToVector2Int(side);
                if (map[positionToMove.x, positionToMove.y] == -1)
                    availableSides.Add(side);
            }
            if (availableSides.Count == 0) // if dead end
            {
                correctPathRoomsPositions.Remove(currentPosition);
                StepBack();
            }
            else
            {
                Direction.Side stepDirrection = availableSides[Random.Range(0, availableSides.Count)]; // random available side
                newPosition = currentPosition + Direction.SideToVector2Int(stepDirrection);
                lastRoomID++;
                map[newPosition.x, newPosition.y] = lastRoomID;
                ConnectRoomBlueprints(map[currentPosition.x, currentPosition.y], map[newPosition.x, newPosition.y], stepDirrection);
                allRoomsPositions.Add(lastRoomID, newPosition);
                correctPathRoomsPositions.Add(newPosition);
                currentPosition = newPosition;
            }
        }
        endPosition = currentPosition;
    }

    void StepBack()
    {
        Vector2Int positionToMove = currentPosition;
        int indexMax = -1;
        foreach (Direction.Side side in Direction.sides)
        {
            Vector2Int newPosition = currentPosition + Direction.SideToVector2Int(side);
            if ((map[newPosition.x, newPosition.y] > indexMax) && correctPathRoomsPositions.Contains(newPosition))
            {
                indexMax = map[newPosition.x, newPosition.y];
                positionToMove = newPosition;
            }
        }
        currentPosition = positionToMove;
    }

    void MakeDeadEnds() {
        Vector2Int randomRoomPosition;
        List<Direction.Side> availableSides;
        List<Vector2Int> roomsWithPossibleExitPositions = new List<Vector2Int>(correctPathRoomsPositions);        
        roomsWithPossibleExitPositions.Remove(endPosition);
        Vector2Int newPosition;
        Vector2Int positionToMove;
        while (lastRoomID < numberOfRooms - 1)
        {
            randomRoomPosition = roomsWithPossibleExitPositions[Random.Range(0, roomsWithPossibleExitPositions.Count)];            
            availableSides = new List<Direction.Side>();
            foreach (Direction.Side side in Direction.sides)
            {
                positionToMove = randomRoomPosition + Direction.SideToVector2Int(side);
                if (map[positionToMove.x, positionToMove.y] == -1)
                    availableSides.Add(side);
            }
            if (availableSides.Count != 0)
            {
                Direction.Side randomSide = availableSides[Random.Range(0, availableSides.Count)];
                newPosition = randomRoomPosition + Direction.SideToVector2Int(randomSide);
                lastRoomID++;
                map[newPosition.x, newPosition.y] = lastRoomID;
                ConnectRoomBlueprints(map[randomRoomPosition.x, randomRoomPosition.y], lastRoomID, randomSide);
                allRoomsPositions.Add(lastRoomID, newPosition);
                roomsWithPossibleExitPositions.Add(newPosition);
            }
            else {
                roomsWithPossibleExitPositions.Remove(randomRoomPosition);
            }
        }
    }
    
    void DrawMap() {// for debug only
        Color lineColor;
        for (int i = 0; i < allRoomsPositions.Count; i++) {
            Vector3 playerPosition = GameObject.FindWithTag("Player").transform.position;
            if (correctPathRoomsPositions.Contains(allRoomsPositions[i]))
                lineColor = Color.green;
            else                
                lineColor = Color.red;
            foreach (Direction.Side side in Direction.sides)
            {
                if (Labirint.instance.blueprints[i].rooms.ContainsKey(side))
                    if (Labirint.instance.blueprints[i].rooms[side] != -1)
                    {
                        Debug.DrawRay(playerPosition + new Vector3(allRoomsPositions[i].x - numberOfRooms+0.5f, allRoomsPositions[i].y - numberOfRooms + 0.5f, 0),
                            Direction.SideToVector3(side), lineColor,5f);
                    }
            }
            if (Labirint.instance.blueprints[i].contanerPrefab != null) {
                lineColor = Color.blue;
                Vector3 point = playerPosition + new Vector3(allRoomsPositions[i].x - numberOfRooms + 0.5f, allRoomsPositions[i].y - numberOfRooms + 0.5f, 0);
                Debug.DrawLine(point + (0.25f * Vector3.up) + (0.25f * Vector3.left), point + (0.25f * Vector3.down) + (0.25f * Vector3.right), lineColor, 5f);
                Debug.DrawLine(point + (0.25f * Vector3.down) + (0.25f * Vector3.left), point + (0.25f * Vector3.up) + (0.25f * Vector3.right), lineColor, 5f);
            }
        }
    }

    void ConnectRoomBlueprints(int firstID, int secondID, Direction.Side direction1to2) {
        Labirint.instance.blueprints[firstID].rooms[direction1to2] = secondID;
        Labirint.instance.blueprints[secondID].rooms[Direction.InvertSide(direction1to2)] = firstID;
    }

    void FillRoomPrefabs() {
        List<GameObject> emptyRoomsList = new List<GameObject>(peacefulRoomPrefabs);
        List<GameObject> combatRoomsList = new List<GameObject>(combatRoomPrefabs);
        labirint.blueprints[0].prefab = RandomGameObjectFromList(emptyRoomsList); // 0 index is for starting room, always empty
        if (!roomRepeatAllowed) emptyRoomsList.Remove(labirint.blueprints[0].prefab);
        for (int i = 1; i < numberOfRooms; i++)
        {
            if (allRoomsPositions[i] != endPosition)
            {
                labirint.blueprints[i].prefab = RandomGameObjectFromList(combatRoomsList);
                if (!roomRepeatAllowed) combatRoomsList.Remove(labirint.blueprints[i].prefab);
            }
            else
            {
                labirint.blueprints[i].prefab = RandomGameObjectFromList(emptyRoomsList);
                if (!roomRepeatAllowed) emptyRoomsList.Remove(labirint.blueprints[i].prefab);
                labirint.blueprints[i].exitSceneName = exitSceneName;
            }
        }
        labirint.blueprints[map[endPosition.x, endPosition.y]].exitSceneName = exitSceneName;
    }

    void FillContainers() {
        List<int> containerAvailableRooms = new List<int>(allRoomsPositions.Keys);
        containerAvailableRooms.Remove(0);                                  // no containers in first room
        containerAvailableRooms.Remove(map[endPosition.x, endPosition.y]);  // and last room

        if (containerAvailableRooms.Count < containersPrefabs.Length)
            Debug.LogError("not enough rooms for containtes");
        else
            foreach (GameObject containerPrefab in containersPrefabs)
            {
                int roomForContainerID = containerAvailableRooms[Random.Range(0, containerAvailableRooms.Count - 1)]; 
                labirint.blueprints[roomForContainerID].contanerPrefab = containerPrefab;
                containerAvailableRooms.Remove(roomForContainerID);
            }
    }

    GameObject RandomGameObjectFromList(List<GameObject> prefabList) {
        if (prefabList.Count == 0)
        {
            Debug.LogError("Not enough room prefabs to fill rooms to labirintBuilder");
            return null;
        }
        return prefabList[Random.Range(0, prefabList.Count)]; 
    }

    void FillTreasureRooms()
    {
        List<int> containerAvailableRooms = new List<int>(allRoomsPositions.Keys);
        containerAvailableRooms.Remove(0);                                  // no containers in first room
        containerAvailableRooms.Remove(map[endPosition.x, endPosition.y]);  // and last room
        List<GameObject> containerList = new List<GameObject>(containersPrefabs);
        List<GameObject> trasureRoomPrefabsList = new List<GameObject>(treasureRoomPrefabs);
        bool repeatTreasureRoomPrefabs = (trasureRoomPrefabsList.Count < treasureRoomsNumber);
        bool repeatContainers = (containerList.Count < treasureRoomsNumber);
                
        for (int i = 0; i<treasureRoomsNumber; i++)
            //foreach (GameObject trsureRoomPrefab in treasureRoomPrefabs) {
            if (containerAvailableRooms.Count <= 0)
                Debug.LogError("not enough rooms for containtes");
            else
            {
                GameObject trsureRoomPrefab = trasureRoomPrefabsList[Random.Range(0, trasureRoomPrefabsList.Count)];
                int randomRoomID = containerAvailableRooms[Random.Range(0, containerAvailableRooms.Count)];
                GameObject randomContainer = containerList[Random.Range(0, containerList.Count)];
                labirint.blueprints[randomRoomID].prefab = trsureRoomPrefab;
                labirint.blueprints[randomRoomID].contanerPrefab = randomContainer;
                if (!repeatContainers) containerList.Remove(randomContainer);
                if (!repeatTreasureRoomPrefabs) trasureRoomPrefabsList.Remove(trsureRoomPrefab);
                containerAvailableRooms.Remove(randomRoomID); // to prevent 2 treasure room placement in same room
            }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) // Alt+M => DrawMap
            DrawMap();
    }

    private void SeedCheck() {
        seed = PlayerPrefs.GetString("seed");
        if (seed != "")
        {
            Random.InitState(seed.GetHashCode() + SceneManager.GetActiveScene().name.GetHashCode());
            Debug.Log("seed = "+seed);
        }
        else
            Debug.Log("no seed");
    }

    static public void SetupSeed(string seedInput) {
        seed = seedInput;
        PlayerPrefs.SetString("seed", seed);
        PlayerPrefs.Save();
    }

    static public void ResetSeed() {
        seed = "";
        PlayerPrefs.SetString("seed", seed);
        PlayerPrefs.Save();
    }
}
