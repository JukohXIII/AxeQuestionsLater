using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;

public class MovesList : MonoBehaviour
{
    [SerializeField] private TMP_Text movesText;
    [SerializeField, TextArea(10, 20)] private string template =
    "Combo:  {attack}, {attack}, {attack}\n\n" +
    "Uptilt:  {up} + {attack}\n\n" +
    "Downtilt:  {down} + {attack}\n\n" +
    "Air attacks:  {jump} + {attack} + {up}/{down}/{left}/{right}\n\n" +
    "Double jump:  {jump} + {jump}\n\n" +
    "Drop through:  {down} + {jump}\n\n" +
    "Fast fall:  {down} while falling\n\n" +
    "Dash attack:  {dash} + {attack}\n\n" +
    "Grab / Throw:  {grab} + {up}/{down}/{left}/{right}\n\n" +
    "Guard:  hold {guard}";

    [Header("Binding rows")]
    [SerializeField] private RebindActionUI up;
    [SerializeField] private RebindActionUI down;
    [SerializeField] private RebindActionUI left;
    [SerializeField] private RebindActionUI right;
    [SerializeField] private RebindActionUI jump;
    [SerializeField] private RebindActionUI attack;
    [SerializeField] private RebindActionUI dash;
    [SerializeField] private RebindActionUI grab;
    [SerializeField] private RebindActionUI guard;

    void OnEnable()
    {
        InputSystem.onActionChange += OnActionChange;
        Refresh();
    }

    void OnDisable()
    {
        InputSystem.onActionChange -= OnActionChange;
    }

    void OnActionChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.BoundControlsChanged) Refresh();
    }

    string Key(RebindActionUI row)
    {
        if (!row.ResolveActionAndBinding(out InputAction action, out int index)) return "?";
        return action.GetBindingDisplayString(index);
    }

    void Refresh()
    {
        movesText.text = template
            .Replace("{attack}", Key(attack))
            .Replace("{up}", Key(up))
            .Replace("{down}", Key(down))
            .Replace("{left}", Key(left))
            .Replace("{right}", Key(right))
            .Replace("{jump}", Key(jump))
            .Replace("{dash}", Key(dash))
            .Replace("{grab}", Key(grab))
            .Replace("{guard}", Key(guard));
    }
}