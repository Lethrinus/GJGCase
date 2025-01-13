using UnityEngine;
using System;

public class InputHandler : MonoBehaviour
{
    public event Action<GameObject> OnBlockClicked;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Camera.main is not null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
                OnBlockClicked?.Invoke(hit.collider.gameObject);
        }
    }
}