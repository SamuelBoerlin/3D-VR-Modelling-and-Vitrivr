using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabberLogic : MonoBehaviour
{
    [SerializeField] private string triggerInput = "";

    [SerializeField] private float grabDistance = 5.0f;

    private Vector3 lastGrabberPos;

    private void Update()
    {
        if (Input.GetAxis(triggerInput) > 0.5)
        {
            GameObject sculpture = GameObject.FindGameObjectWithTag("Sculpture");
            if (sculpture != null)
            {
                Sculpture script = sculpture.GetComponent<Sculpture>();
                if (script != null)
                {
                    if (script.RayCast(transform.position, transform.rotation * new Vector3(0, 0, 1), grabDistance, out Sculpture.RayCastResult result))
                    {
                        sculpture.transform.SetParent(this.gameObject.transform);
                        Rigidbody rb = sculpture.GetComponent<Rigidbody>();
                        if (rb != null)
                            rb.isKinematic = true;
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        var transform = this.gameObject.GetComponent<Transform>();

        Vector3 velocity = (transform.position - lastGrabberPos) / Time.fixedDeltaTime;

        if (Input.GetAxis(triggerInput) < 0.5)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                child.SetParent(null, true);
                Rigidbody rb = child.gameObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.velocity = velocity;
                }
            }
        }

        this.lastGrabberPos = transform.position;
    }
}
