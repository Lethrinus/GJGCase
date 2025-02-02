using UnityEngine;
using System;

public class InputHandler : MonoBehaviour
{
    public event Action<BlockBehavior> OnBlockClicked;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Camera.main)
        {
            Vector2 mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mp, Vector2.zero);
            if (hit.collider)
            {
                var b = hit.collider.GetComponent<BlockBehavior>();
                if (b) OnBlockClicked?.Invoke(b);
            }
        }
    }
}