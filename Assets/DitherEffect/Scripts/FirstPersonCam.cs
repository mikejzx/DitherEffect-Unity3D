using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonCam : MonoBehaviour
{
    public float moveSpeed = 7.0f;
    public float updownSpeed = 6.0f;
    public float yawSpeed = 4.0f;
    public float pitchSpeed = 3.0f;

    private Vector2 mouseRot;

    private bool firstFrame = true;

    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.Euler(new Vector3(0.0f, transform.eulerAngles.y, 0.0f));

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        firstFrame = true;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 forward = transform.forward;
        forward.y = 0.0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0.0f;
        right.Normalize();

        Vector3 up = transform.up;

        if (Input.GetKey(KeyCode.W))
            transform.position += forward * moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.S))
            transform.position -= forward * moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.A))
            transform.position -= right * moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.D))
            transform.position += right * moveSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.Q))
            transform.position -= up * updownSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.E))
            transform.position += up * updownSpeed * Time.deltaTime;

        if (!firstFrame)
        {
            // Apply yaw and pitch
            mouseRot.x -= Input.GetAxis("Mouse Y") * pitchSpeed;
            mouseRot.y += Input.GetAxis("Mouse X") * yawSpeed;
            transform.localRotation = Quaternion.Euler(mouseRot);
        }

        firstFrame = false;
    }
}
