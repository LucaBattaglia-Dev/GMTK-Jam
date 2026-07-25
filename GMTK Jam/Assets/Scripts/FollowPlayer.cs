using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    private Transform playerCapsule; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCapsule = transform.parent.GetChild(1);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(playerCapsule.position.x, transform.position.y, playerCapsule.position.z);
    }
}
