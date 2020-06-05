using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour
{
    public float riseDelay;
    public float riseSpeed;
    public float maxHeight;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < maxHeight) {
            if (riseDelay > 0) {
                riseDelay -= Time.deltaTime;
            }
            else {
                float newHeight;
                if (transform.position.y + riseSpeed * Time.deltaTime < maxHeight)
                    newHeight = riseSpeed * Time.deltaTime;
                else
                    newHeight = maxHeight;

                transform.position = transform.position + new Vector3(0, newHeight, 0);
            }
        }
    }
}
