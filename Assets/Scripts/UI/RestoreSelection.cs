using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// When the mouse leaves everything un-highlighted, the first arrow/stick press brings the highlight back.
public class RestoreSelection : MonoBehaviour
{
    private EventSystem es;
    private InputSystemUIInputModule module;
    private GameObject last;

    void Awake()
    {
        es = GetComponent<EventSystem>();
        module = GetComponent<InputSystemUIInputModule>();
    }

    void Update()
    {
        GameObject current = es.currentSelectedGameObject;
        if (current != null) { last = current; return; }

        bool navPressed = module.move.action.ReadValue<Vector2>().sqrMagnitude > 0.25f;
        if (navPressed && last != null && last.activeInHierarchy)
            es.SetSelectedGameObject(last);
    }
}