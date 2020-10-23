using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantMovement : MonoBehaviour
{
    public float speed;
    private void FixedUpdate()
    {
        transform.position = transform.position + Vector3.forward * speed * Time.fixedDeltaTime;
    }
}
