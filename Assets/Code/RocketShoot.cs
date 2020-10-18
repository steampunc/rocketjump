using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketShoot : MonoBehaviour
{
    public float rocketSpeed = 18f;
    public GameObject bombPrefab;
    private Camera playerCam;

    // Start is called before the first frame update
    void Start()
    {
        playerCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            GameObject newBomb = Instantiate(bombPrefab, playerCam.transform.position + Vector3.down * 0.2f + playerCam.transform.forward * 1f, playerCam.transform.rotation);
            Physics.IgnoreCollision(newBomb.GetComponent<Collider>(), GetComponent<Collider>());
            newBomb.GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * rocketSpeed, ForceMode.VelocityChange);
            /*
            RaycastHit hit;
            int layerMask = 1 << 8;

            layerMask = ~layerMask;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask)) {
                Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * hit.distance, Color.yellow);
                ApplyExplosion(transform.position + hit.transform.position);
            }*/
            //Debug.Log("Clicked");
        }
    }
    
}
