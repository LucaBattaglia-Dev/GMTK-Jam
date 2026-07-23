using System;
using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody carRB;
    [SerializeField] private Transform[] rayPoints;
    [SerializeField] private LayerMask drivable;

    [Header("Suspension Settings")]
    [SerializeField] private float springStiffness;
    [SerializeField] private float damperStiffness;
    [SerializeField] private float restLength;
    [SerializeField] private float springTravel;
    [SerializeField] private float wheelRadius;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        carRB = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Suspension();
    }

    // Update is called once per frame
    private void Suspension()
    {
        foreach (Transform rayPoint in rayPoints)
        {
            RaycastHit hit;
            float maxLength = restLength + springTravel;

            if (Physics.Raycast(rayPoint.position, -rayPoint.up, out hit, maxLength + wheelRadius, drivable))
            {
                float currentSpringLength = hit.distance - wheelRadius;
                float springCompression = (restLength - currentSpringLength) / springTravel;

                float springVelocity = Vector3.Dot(carRB.GetPointVelocity(rayPoint.position), rayPoint.up);
                float dampForce = damperStiffness * springVelocity;

                float springForce = springCompression * springStiffness;

                float netForce = springForce - dampForce;

                carRB.AddForceAtPosition(netForce * rayPoint.up, rayPoint.position);

                Debug.DrawLine(rayPoint.position, hit.point, Color.red);

            }
            else
            {
                Debug.DrawLine(rayPoint.position, rayPoint.position + (wheelRadius + maxLength) * -rayPoint.up, Color.green);
            }
        }
    }
}
