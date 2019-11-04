using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRGuiPointer : MonoBehaviour
{
    [SerializeField] private string clickInput = "";

    [SerializeField] private Canvas canvas = null;

    private GraphicRaycaster rayCaster = null;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = canvas.GetComponent<RectTransform>();
        rayCaster = canvas.GetComponent<GraphicRaycaster>();
    }

    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(new Ray(Vector3.zero, new Vector3(0, 0, 1)));

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
        if (Input.GetAxis(clickInput) > 0.5)
        {
            Plane plane = new Plane(canvas.transform.rotation * new Vector3(0, 0, 1), canvas.transform.position);

            Ray ray = new Ray(transform.position, transform.rotation * new Vector3(0, 0, 1));

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 point = ray.GetPoint(enter);

                Vector3 canvasRight = canvas.transform.rotation * new Vector3(1, 0, 0);
                Vector3 canvasDown = canvas.transform.rotation * new Vector3(0, -1, 0);

                Matrix4x4 canvasScale = Matrix4x4.Scale(canvas.transform.localScale);

                float canvasWidth = (canvasScale * canvasRight * rectTransform.rect.width).magnitude;
                float canvasHeight = (canvasScale * canvasDown * rectTransform.rect.height).magnitude;

                Vector2 canvasPoint = new Vector2(
                    (Vector3.Dot(point - canvas.transform.position, canvasRight) / canvasWidth + 0.5f) * rectTransform.rect.width,
                    (Vector3.Dot(point - canvas.transform.position, canvasDown) / canvasHeight + 0.5f) * rectTransform.rect.height
                    );

                PointerEventData ped = new PointerEventData(EventSystem.current);

                ped.position = canvasPoint;

                List<RaycastResult> results = new List<RaycastResult>();

                rayCaster.Raycast(ped, results);

                Debug.Log("Click");
                foreach(RaycastResult result in results) {
                    Debug.Log(results);
                }

                Gizmos.DrawSphere(point, 0.05f);
            }
        }
    }
}
