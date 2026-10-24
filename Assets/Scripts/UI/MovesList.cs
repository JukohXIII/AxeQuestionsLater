using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;

public class MovesList : MonoBehaviour
{
    [SerializeField] private TMP_Text movesText;

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
        string a = Key(attack);
        string u = Key(up);
        string d = Key(down);

        var sb = new StringBuilder();
        sb.AppendLine($"Combo:  {a}, {a}, {a}");
        sb.AppendLine($"Uptilt:  {u} + {a}");
        sb.AppendLine($"Downtilt:  {d} + {a}");
        sb.AppendLine($"Air attacks:  {a} in the air (+ direction)");
        sb.AppendLine($"Double jump:  {Key(jump)} in the air");
        sb.AppendLine($"Drop through:  {d} + {Key(jump)}");
        sb.AppendLine($"Fast fall:  {d} while falling");
        sb.AppendLine($"Dash attack:  {Key(dash)}, then {a}");
        sb.AppendLine($"Grab / Throw:  {Key(grab)}, then a direction");
        sb.AppendLine($"Guard:  hold {Key(guard)}");
        movesText.text = sb.ToString();
    }
}