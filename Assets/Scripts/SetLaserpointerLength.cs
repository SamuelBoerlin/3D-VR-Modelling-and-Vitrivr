using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
public class SetLaserpointerLength : MonoBehaviour
{
    [SerializeField] private GameObject tip = null;
    [SerializeField] private float maxDistance = 8.0f;

    private LineRenderer lineRenderer = null;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        float distance = maxDistance;
        if(Physics.Raycast(new Ray(transform.position, transform.rotation * new Vector3(0, 0, 1)), out RaycastHit hit, maxDistance))
        {
            distance = hit.distance;
        }
        lineRenderer.SetPosition(1, new Vector3(0, 0, distance));
        tip.transform.localPosition = new Vector3(0, 0, distance);
    }
}
