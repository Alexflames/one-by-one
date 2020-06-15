﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopRotation : MonoBehaviour
{
    [SerializeField]
    public Vector3 offset = new Vector3(0, 0);

    [SerializeField]
    public Vector3 baseEulerRotation = new Vector3(0, 0, 0);

    void Awake()
    {
        savedAngles = transform.localEulerAngles;
        if (offset == Vector3.zero) offset = transform.localPosition;
    }

    void Start()
    {
        transform.localEulerAngles = savedAngles;
        if (offset == Vector3.zero) offset = transform.localPosition;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.eulerAngles = savedAngles;
        transform.position = transform.parent.position + offset;
    }

    private Vector3 savedAngles = Vector3.zero;
}
