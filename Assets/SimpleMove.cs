using UnityEngine;

public class SimpleMove : MonoBehaviour {
    void Update() {
        float speed = 5.0f;
        if(Input.GetMouseButton(1)) { // Hold right click to look
            transform.Rotate(0, Input.GetAxis("Mouse X") * 2, 0);
        }
        float moveX = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        transform.Translate(moveX, 0, moveZ);
    }
}