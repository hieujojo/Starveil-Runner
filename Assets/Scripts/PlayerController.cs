using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Rigidbody của player
    private Rigidbody rb;

    // Di chuyển theo trục X và Y
    private float movementX;
    private float movementY;

    // Tốc độ di chuyển
    public float speed = 0;

    void Start()
    {
        // Lấy component Rigidbody gắn vào player
        rb = GetComponent<Rigidbody>();
    }

    void OnMove(InputValue movementValue)
    {
        // Chuyển input thành Vector2
        Vector2 movementVector = movementValue.Get<Vector2>();

        // Lưu giá trị X và Y
        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    void FixedUpdate()
    {
        // Tạo vector di chuyển 3D
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);

        // Áp lực vào Rigidbody
        rb.AddForce(movement * speed);
    }
}