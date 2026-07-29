using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Tham chiếu đến Player
    public GameObject player;

    // Khoảng cách giữa camera và player
    private Vector3 offset;

    void Start()
    {
        // Tính offset ban đầu
        offset = transform.position - player.transform.position;
    }

    void LateUpdate()
    {
        // Giữ nguyên khoảng cách theo player
        transform.position = player.transform.position + offset;
    }
}