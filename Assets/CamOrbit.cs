using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamOrbit : MonoBehaviour
{
    [SerializeField]
    Transform focus;

    Vector3 focusPoint;

    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    Vector2 orbitAngle;

    [SerializeField, Range(0f, 1f)]
    float focusCentering = 0.5f;

    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;

    [SerializeField, Range(0.1f, 5f)]
    float sensitivity = 1f;

    void Awake()
    {
        focusPoint = focus.position;
        orbitAngle = new Vector2(45f, 0f);
    }

    void LateUpdate()
    {
        UpdateFocusPoint();
        ManualRotation();
        Quaternion lookRotation = Quaternion.Euler(orbitAngle.x, orbitAngle.y, 0);

        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;

        transform.position = lookPosition;
        transform.rotation = lookRotation;
    }

    void ManualRotation()
    {
        Vector2 input = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );

        if (input.sqrMagnitude > 0.001f)
        {
            orbitAngle.y += input.x * rotationSpeed * Time.unscaledDeltaTime * sensitivity;
            orbitAngle.x -= input.y * rotationSpeed * Time.unscaledDeltaTime * sensitivity;
            orbitAngle.x = Mathf.Clamp(orbitAngle.x, -89f, 89f); // Clamp vertical angle
        }
    }

    void UpdateFocusPoint()
    {
        Vector3 targetPoint = focus.position;

        if (focusRadius > 0f)
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;

            if (distance > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }

            if (distance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance);
            }

            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        }
        else
        {
            focusPoint = targetPoint;
        }
    }
}
