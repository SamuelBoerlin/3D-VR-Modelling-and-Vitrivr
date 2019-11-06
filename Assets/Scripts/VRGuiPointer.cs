using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRGuiPointer : MonoBehaviour
{
    [SerializeField] private string clickInput = "";

    [SerializeField] public Transform handTransform = null;

    [SerializeField] public Camera camera = null;

    [SerializeField] public GuiManager guiManager = null;

    private GraphicRaycaster rayCaster = null;

    private RectTransform rectTransform;

    private bool wasTriggerPressed = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        rayCaster = GetComponent<GraphicRaycaster>();
    }

    void Update()
    {
        EventSystem.current.SetSelectedGameObject(null);

        bool isTriggerPressed = Input.GetAxis(clickInput) > 0.5;

        Plane plane = new Plane(transform.rotation * new Vector3(0, 0, 1), transform.position);

        Ray ray = new Ray(handTransform.position, handTransform.rotation * new Vector3(0, 0, 1));

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 point = ray.GetPoint(enter);

            Vector3 canvasRight = transform.rotation * new Vector3(1, 0, 0);
            Vector3 canvasDown = transform.rotation * new Vector3(0, -1, 0);

            Matrix4x4 canvasScale = Matrix4x4.Scale(transform.localScale);

            float canvasWidth = (canvasScale * canvasRight * rectTransform.rect.width).magnitude;
            float canvasHeight = (canvasScale * canvasDown * rectTransform.rect.height).magnitude;

            Vector2 canvasPoint = new Vector2(
                (Vector3.Dot(point - transform.position, canvasRight) / canvasWidth + 0.5f) * rectTransform.rect.width,
                (Vector3.Dot(point - transform.position, canvasDown) / canvasHeight + 0.5f) * rectTransform.rect.height
                );

            PointerEventData ped = new PointerEventData(EventSystem.current);

            Vector3 wp = camera.WorldToScreenPoint(point);

            ped.position = wp;

            List<RaycastResult> results = new List<RaycastResult>();

            rayCaster.Raycast(ped, results);

            foreach (RaycastResult result in results)
            {
                EventSystem.current.SetSelectedGameObject(result.gameObject);
            }
        }

        if (isTriggerPressed != wasTriggerPressed)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null)
            {
                Debug.Log("Click: " + selected);
                ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }
        }
        wasTriggerPressed = isTriggerPressed;
    }
}
