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

    [Header("Controller rows")]
    [SerializeField] private GameObject gamepadList;
    [SerializeField] private RebindActionUI padJump;
    [SerializeField] private RebindActionUI padAttack;
    [SerializeField] private RebindActionUI padDash;
    [SerializeField] private RebindActionUI padGrab;
    [SerializeField] private RebindActionUI padGuard;

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

    public void Refresh()
    {
        bool pad = gamepadList.activeSelf;

        movesText.text = template
            .Replace("{attack}", Key(pad ? padAttack : attack))
            .Replace("{jump}", Key(pad ? padJump : jump))
            .Replace("{dash}", Key(pad ? padDash : dash))
            .Replace("{grab}", Key(pad ? padGrab : grab))
            .Replace("{guard}", Key(pad ? padGuard : guard))
            .Replace("{up}", pad ? "Up" : Key(up))
            .Replace("{down}", pad ? "Down" : Key(down))
            .Replace("{left}", pad ? "Left" : Key(left))
            .Replace("{right}", pad ? "Right" : Key(right));
    }
}