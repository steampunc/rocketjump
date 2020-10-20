using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketShoot : MonoBehaviour
{
    public GameObject bombPrefab;
    private Camera playerCam;

    public float shootCooldown = 0.8f;
    private float currentCooldown;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        playerCam = Camera.main;
        currentCooldown = 0;
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }
        if (Input.GetMouseButton(0) && currentCooldown <= 0) {
            GameObject newBomb = Instantiate(bombPrefab, playerCam.transform.position + Vector3.down * 0.3f + playerCam.transform.forward, playerCam.transform.rotation);
            Physics.IgnoreCollision(newBomb.GetComponent<Collider>(), GetComponent<Collider>());
            /*
            RaycastHit hit;
            int layerMask = 1 << 8;

            layerMask = ~layerMask;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Mathf.Infinity, layerMask)) {
                Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * hit.distance, Color.yellow);
                ApplyExplosion(transform.position + hit.transform.position);
            }*/
            //Debug.Log("Shot rocket");

            audioSource.Play();
            currentCooldown = shootCooldown;
        }
    }
    
}
