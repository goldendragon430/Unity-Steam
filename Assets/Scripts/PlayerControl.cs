using Unity.Netcode;
using UnityEngine;

public class PlayerControl : NetworkBehaviour
{
    [SerializeField]
    private float speed = 0.5f;
    private Vector3 serverpostion = new Vector3(-5, 2, -13);
    private Vector3 clientpostion = new Vector3(5, 2, -13);
    void Update()
    {
        if (!IsOwner) return ;
        float xInput = Input.GetAxis("Horizontal");
        float yInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(xInput, 0, yInput).normalized;
        transform.Translate(speed * Time.deltaTime * moveDirection);
    }
     
    private void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            transform.position = serverpostion;
        }
    }
}