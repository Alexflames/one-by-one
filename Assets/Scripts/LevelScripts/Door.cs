﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    [HideInInspector] public Room room;
    [HideInInspector] public Door connectedDoor;
    public bool locked = false; 
    private GameObject player;

    [HideInInspector] public bool unlockOnTimer = false;
    private float timer = 1f;
    
    [SerializeField] public Direction.Side direction;
    public string sceneName=""; // name of scene to change on enter this door
    public bool isSpawned = false;
    private SpriteRenderer spriteRenderer;
    private Transform doorVisual;

    [SerializeField]
    private GameObject arrowSprite;

    void Awake()
    {
        doorVisual = transform.GetChild(0);
        spriteRenderer = doorVisual.GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        switch (direction)
        {
            case Direction.Side.LEFT:
                doorVisual.localPosition += Vector3.left * 3;
                break;
            case Direction.Side.UP:
                doorVisual.Rotate(0, 0, -90);
                doorVisual.localPosition += Vector3.up * 3;
                break;
            case Direction.Side.DOWN:
                doorVisual.Rotate(0, 0, 90);
                doorVisual.localPosition += Vector3.down * 3;
                break;
            case Direction.Side.RIGHT:
                doorVisual.Rotate(0, 0, 180);
                doorVisual.localPosition += Vector3.right * 3;
                break;
            default:
                break;
        }
    }

    void Update()
    {
        if (isSpawned && unlockOnTimer && locked) {
            timer -= Time.deltaTime;
            if (timer <= 0) {
                Unlock();
            }
        }
        ArrowCheck();
    }

    private void OnTriggerStay2D(Collider2D collision) // "Stay" needed to make door work if player was on trigger in moment of unlock
    {
        if (isSpawned && !locked && collision.gameObject == player) {
            if (sceneName == "") {
                connectedDoor.room.MoveToRoom(connectedDoor);
            } else {
                Metrics.OnWin();
                RelodScene.OnSceneChange?.Invoke();
                SceneManager.LoadScene(sceneName);
            }
        }
    }

    public void Unlock() {
        if (locked && isSpawned)
        {
            locked = false;
            if (!Labirint.instance || !Labirint.instance.blueprints[room.roomID].visited)
            {
                foreach (var animation in GetComponentsInChildren<Animation>())
                {
                    animation.Play();
                }
                foreach (var animation in GetComponentsInChildren<Animator>())
                {
                    animation.Play("Open");
                }
            }
        }
    }

    public void Lock()
    {
        if (isSpawned)
        {
            locked = true;
            //animation?
            timer = 1f;
        }
        else
        {
            foreach (var animation in GetComponentsInChildren<Animator>())
            {
                animation.Play("Close");
            }
        }
    }

    public void SpawnDoor() {
        if (!isSpawned)
        {
            isSpawned = true;
            doorVisual.gameObject.SetActive(true);
        }
    }

    public bool directionAutoset() {
        if (sceneName == "")
        {
            if (transform.localPosition.x / (Mathf.Abs(transform.localPosition.y) + 0.001f) > 2)    // эврестическая оценка направления от центра на дверь, оверрайдится из инспектора
            {                                                                                       // если дверь на правой стене x/|y|>2 в локальных координатах то дверь правая, и т.п.
                direction = Direction.Side.RIGHT;
            }
            else if (transform.localPosition.x / (Mathf.Abs(transform.localPosition.y) + 0.001f) < -2) // + 0.001f - защита от деления на ноль
            {
                direction = Direction.Side.LEFT;
            }
            else if (transform.localPosition.y / (Mathf.Abs(transform.localPosition.x) + 0.001f) > 2)
            {
                direction = Direction.Side.UP;
            }
            else if (transform.localPosition.y / (Mathf.Abs(transform.localPosition.x) + 0.001f) < -2)
            {
                direction = Direction.Side.DOWN;
            }
            else
            {
                Debug.LogWarning("Cant get door dirrection automaticaly. Need to set it in inspector manually.");
                return false;
            }
            return true;
        }
        return false;
    }
    
    private void OnDrawGizmos()
    {
        if (isSpawned)
        {
            Gizmos.color = Color.green; // green box for trigger
            Gizmos.DrawWireCube(transform.position + (Vector3)GetComponent<BoxCollider2D>().offset, GetComponent<BoxCollider2D>().size);

            BoxCollider2D blocker = null; // red box for blocker
            foreach (BoxCollider2D coll in GetComponentsInChildren<BoxCollider2D>())
            {
                if (coll.gameObject != gameObject) blocker = coll;
            }
            if (blocker != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(blocker.transform.position + (Vector3)blocker.offset, blocker.size);
            }

            if (connectedDoor != null)
            { // blue line for connection between doors
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, connectedDoor.transform.position);
            }

            if (locked)
            {
                Gizmos.color = Color.red; // red circle for locked door
                Gizmos.DrawWireSphere(transform.position, 1);
            }
            else
            {
                Gizmos.color = Color.green; // green circle for unlocked door, and spawn position
                Gizmos.DrawWireSphere(transform.position, 1);
            }
        }
    }


    private void ArrowCheck() {
        if (room.roomID == Labirint.instance.currentRoomID && isSpawned && locked == false)
        {
            bool arrowNeeled = false;
            float arrowRotation = 0;
            Camera camera = Camera.main;
            Vector3 viewportPosition = camera.WorldToViewportPoint(transform.position);
            Vector3 arrowPosition = Vector3.zero;
            float shiftFromScreenBorder = 1f;
            if (viewportPosition.x < 0)
            {
                arrowNeeled = true;
                arrowRotation = 0;
                if (viewportPosition.y < 0)
                {
                    arrowPosition = camera.ViewportToWorldPoint(Vector3.zero) + shiftFromScreenBorder * (Vector3.right + Vector3.up);
                }
                else if (viewportPosition.y > 1)
                {
                    arrowPosition = camera.ViewportToWorldPoint(Vector3.up) + shiftFromScreenBorder * (Vector3.right + Vector3.down);
                }
                else {
                    arrowPosition = new Vector3(camera.ViewportToWorldPoint(Vector3.zero).x + shiftFromScreenBorder, transform.position.y, 0);
                }
            }
            else if (viewportPosition.x > 1)
            {
                arrowNeeled = true;
                arrowRotation = 180;
            }
            else if (viewportPosition.y < 0)
            {
                arrowNeeled = true;
                arrowRotation = 90;
            }
            else if (viewportPosition.y > 1)
            {
                arrowNeeled = true;
                arrowRotation = 270;
            }

            if (arrowSprite.activeSelf != arrowNeeled)
                arrowSprite.SetActive(arrowNeeled);
            if (arrowNeeled)
            {
                arrowSprite.transform.rotation = Quaternion.Euler(0, arrowRotation, 0);
                arrowSprite.transform.position = arrowPosition;
            }
        }
    }
}
