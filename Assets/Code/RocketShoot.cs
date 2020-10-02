using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketShoot : MonoBehaviour
{
    public GameObject bombPrefab;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            var newBomb = Instantiate(bombPrefab, transform.position + Vector3.up * 3, Quaternion.identity);
            newBomb.AddForce(Vector3.down, ForceMode.Impulse);
            /*
            RaycastHit hit;
            int layerMask = 1 << 8;

            layerMask = ~layerMask;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask)) {
                Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * hit.distance, Color.yellow);
                ApplyExplosion(transform.position + hit.transform.position);
            }*/
            Debug.Log("Clicked");
        }
    }
    
}
