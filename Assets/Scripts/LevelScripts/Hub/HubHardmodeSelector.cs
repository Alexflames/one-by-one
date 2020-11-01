﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

public class HubHardmodeSelector : MonoBehaviour
{
    private const string HARDMODE = "2";
    private const string NORMALMODE = "1";

    private string difficulty;

    public GameObject hardmodeVisualContainer;
    public GameObject canvasHardmodeSelector;
    public TMPro.TextMeshPro hubHardmodeText;
    public TMPro.TextMeshProUGUI canvasHardmodeText;

    private const string hardmodeText = "Hardmode: ON";
    private const string nonHardmodeText = "Hardmode: OFF";

    void Start()
    {
        CheckHardmodeStatus();
    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        if (coll.CompareTag("Player"))
        {
            canvasHardmodeSelector.SetActive(true);
            Cursor.visible = true;
            CharacterShooting.GetCursor().gameObject.SetActive(false);
        }
    }

    private void OnTriggerExit2D(Collider2D coll)
    {
        if (coll.CompareTag("Player"))
        {
            canvasHardmodeSelector.SetActive(false);
            Cursor.visible = false;
            CharacterShooting.GetCursor().gameObject.SetActive(true);
        }
    }

    public void UpdateHardmodeStatus()
    {
        if (difficulty == HARDMODE) PlayerPrefs.SetString("Gamemode", NORMALMODE);
        else if (difficulty == NORMALMODE) PlayerPrefs.SetString("Gamemode", HARDMODE);
        else throw new System.Exception("Unexpected difficulty/gamemod value");

        CheckHardmodeStatus();
    }

    private void CheckHardmodeStatus()
    {
        difficulty = PlayerPrefs.GetString("Gamemode");
        if (difficulty == HARDMODE)
            SetHardmodeVisual();
        else if (difficulty == NORMALMODE)
            SetNonHardmodeVisual();
        else
            throw new System.Exception("Unexpected difficulty/gamemod value");
    }

    private void SetHardmodeVisual()
    {
        hardmodeVisualContainer.SetActive(true);
        hubHardmodeText.text = hardmodeText;
        canvasHardmodeText.text = hardmodeText;
    }

    private void SetNonHardmodeVisual()
    {
        hardmodeVisualContainer.SetActive(false);
        hubHardmodeText.text = nonHardmodeText;
        canvasHardmodeText.text = nonHardmodeText;
    }
}