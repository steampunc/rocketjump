using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnIfTouched : MonoBehaviour
{
    private Rigidbody rb;
    private float yBoundExtent;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        yBoundExtent = GetComponent<Collider>().bounds.extents.y;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.tag == "Respawn")
        {
            Debug.Log("feetheight: " + (rb.position.y - yBoundExtent) + "   aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            rb.velocity = Vector3.zero;
            transform.position = GameObject.Find("Spawnpoint").transform.position;
        }
    }
}
