﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentEnemyUI : MonoBehaviour
{
    [SerializeField]
    GameObject CanvasPrefab = null;

    // Start is called before the first frame update
    void Awake()
    {
        canvasEnemyName = Instantiate(CanvasPrefab);
        if (GameObject.FindGameObjectWithTag("Room") != null) {  // in labirint mode, to delete with parent
            canvasEnemyName.transform.SetParent(transform); // отключить!!!!!
        }
        var canvasScript = canvasEnemyName.GetComponent<Canvas>();
        //canvasScript.worldCamera = Camera.main.transform.GetChild(0).GetComponent<Camera>();
        canvasScript.sortingLayerName = "OnEffect";
        EnemyName = canvasEnemyName.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        EnemyName.GetComponent<Canvas>().sortingLayerName = "OnEffect";
        enemyNameUICenter = canvasEnemyName.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>();
        sizeY = enemyNameUICenter.sizeDelta.y;
    }

    private void Update()
    {
        timeSinceLastNewName = Mathf.Clamp01(timeSinceLastNewName + (Time.deltaTime / transitionTime));
        if (timeSinceLastNewName < 0.5f)
        {
            EnemyName.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, timeSinceLastNewName * 2));
            EnemyName.text = oldCurrentEnemy;
        }
        else
        {
            EnemyName.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, (timeSinceLastNewName - 0.5f) * 2));
            EnemyName.text = newCurrentEnemy;
        }
        enemyNameUICenter.sizeDelta = new Vector2(Mathf.Lerp(oldUIwidth, newUIwidth, timeSinceLastNewName), sizeY);
    }

    public static void SetCurrentEnemy(GameObject enemy)
    {
        var text = enemy.GetComponent<TMPro.TextMeshPro>().text;
        SetCurrentEnemy(text);
    }

    public static void SetCurrentEnemy(string enemyName)
    {
        timeSinceLastNewName = 0;
        oldUIwidth = enemyNameUICenter.sizeDelta.x;
        newUIwidth = Mathf.LerpUnclamped(40, 120, (enemyName.Length / 7f));

        oldCurrentEnemy = newCurrentEnemy;
        newCurrentEnemy = enemyName;
    }

    public static GameObject GetCanvasInstance() => canvasEnemyName;

    private const float transitionTime = 0.35f;
    private static float timeSinceLastNewName = 1;
    private static float newUIwidth = 40;
    private static float oldUIwidth = 40;
    private static string oldCurrentEnemy;
    private static string newCurrentEnemy;

    private GameObject gameController;
    private static RectTransform enemyNameUICenter = null;
    public static TMPro.TextMeshProUGUI EnemyName;
    private static GameObject canvasEnemyName;
    private float sizeY;
}
