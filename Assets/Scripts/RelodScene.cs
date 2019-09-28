﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class RelodScene : MonoBehaviour
{
    [SerializeField]
    public int pointsToVictory;
    [SerializeField]
    protected int SceneNumber = 0;
    public static bool isVictory = false;
    public int TotalValue = 0;
    protected float maxvalue = 0;
    [SerializeField]
    protected string NextSceneName = "";
    protected static GameObject Canvas;
    [SerializeField]
    public bool isPoint = false;

    private void Start()
    {
        if (isPoint) 
        {
            ArenaEnemySpawner spawn = GetComponent<ArenaEnemySpawner>();
            spawn.isPoint = true;
        }
        CharacterLife.isDeath = false;
        Canvas = GameObject.FindGameObjectWithTag("Canvas");
        var arena = GetComponent<ArenaEnemySpawner>();
        Canvas.transform.GetChild(0).gameObject.SetActive(false);
        maxvalue = arena.EnemyCount();
    }

    public void CurrentCount(int val)
    {
        TotalValue = TotalValue + val;
    }

    private void Update()
    {
        Victory();
        Reload();
    }

    protected virtual void Victory()
    {
        if (!isPoint)
        {
            if (TotalValue == maxvalue)
            {
                isVictory = true;
                Canvas.transform.GetChild(0).gameObject.SetActive(true);
                if (Input.GetKeyDown(KeyCode.F) && !CharacterLife.isDeath)
                {
                    if (PlayerPrefs.GetInt("CurrentScene") < SceneNumber + 1)
                        PlayerPrefs.SetInt("CurrentScene", SceneNumber + 1);
                    Canvas.transform.GetChild(0).gameObject.SetActive(false);
                    SceneManager.LoadScene(NextSceneName);
                }
            }
        }
        else
        {
            if (TotalValue == pointsToVictory)
            {
                isVictory = true;
                Canvas.transform.GetChild(0).gameObject.SetActive(true);
                if (Input.GetKeyDown(KeyCode.F) && !CharacterLife.isDeath)
                {
                    if (PlayerPrefs.GetInt("CurrentScene") < SceneNumber + 1)
                        PlayerPrefs.SetInt("CurrentScene", SceneNumber + 1);
                    Canvas.transform.GetChild(0).gameObject.SetActive(false);
                    SceneManager.LoadScene(NextSceneName);
                }
            }
        }
    }

    protected virtual void Reload()
    {
        if (Input.GetKeyDown(KeyCode.R) && (!isVictory || CharacterLife.isDeath))
        {
            TotalValue = 0;
            Time.timeScale = 1;
            Canvas.transform.GetChild(1).gameObject.SetActive(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

public static void PressR()
    {
        if (isVictory && !CharacterLife.isDeath) return;
        Canvas.transform.GetChild(1).gameObject.SetActive(true);
    }
}
