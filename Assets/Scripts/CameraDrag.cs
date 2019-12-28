using UnityEngine;

public class CameraDrag : MonoBehaviour
{
    public float dragSpeed = 2;
    private Vector3 dragOrigin;

    public Transform watchingGO;

    void Update()
    {
        if(watchingGO != null)
        {
            transform.position = new Vector3(
                watchingGO.position.x,
                watchingGO.position.y,
                -10
            );
        }

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Debug.Log("Click");
            if (Physics.Raycast(ray, out hit, 100))
            {
                watchingGO = hit.collider.transform;
                Debug.Log("Hit object: " + hit.collider.name);
            }
        }

        DragAndMove();
    }

    void DragAndMove()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = Input.mousePosition;
            return;
        }

        if (!Input.GetMouseButton(1)) return;

        Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
        Vector3 move = new Vector3(pos.x * dragSpeed * Time.deltaTime, pos.y * dragSpeed * Time.deltaTime, 0);

        transform.Translate(move, Space.World);
    }

}
