using UnityEngine;
using System.Collections;

/// <summary>
/// Simple camera controller for demo purposes.
/// </summary>
public class DemoCameraController : MonoBehaviour
{
    private const float Speed = 50f;
    private const float ZoomSpeed = 50f;

    void Start()
    {

    }

    void Update()
    {
        var pos = transform.position;

        var h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) > 0.001f)
        {
            pos.x += h * Speed * Time.deltaTime;
        }

        var v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(v) > 0.001f)
        {
            pos.z += v * Speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.Z))
        {
            pos.y += ZoomSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.X))
        {
            pos.y -= ZoomSpeed * Time.deltaTime;
        }

        pos.x = Mathf.Clamp(pos.x, 0, 1024);
        pos.y = Mathf.Clamp(pos.y, 16, 200);
        pos.z = Mathf.Clamp(pos.z, -24, 1000);

        transform.position = pos;
    }
}
