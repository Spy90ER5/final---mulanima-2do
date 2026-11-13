using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] private float velocidad = 90f; // Grados por segundo
    [SerializeField] private Vector3 eje = Vector3.up; // Eje Y (vertical)

    void Update()
    {
        // Multiplicar por Time.deltaTime asegura la misma velocidad en cualquier PC
        transform.Rotate(eje * velocidad * Time.deltaTime);
    }
}
