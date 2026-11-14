using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAxe : MonoBehaviour
{
    [SerializeField] private AxeProjectile axePrefab;
    [SerializeField] private Vector2 spawnOffset = new (0.6f, 0.5f);   // where it leaves his hand
    [SerializeField] private float throwDuration = 0.77f;   // Lars_throwAxe length
    [SerializeField] private float releaseTime = 0.45f;     // frame the axe leaves his hand
    [SerializeField] private float catchDuration = 0.68f;   // Lars_receiveAxe length

    public bool HasAxe => currentAxe == null;
    public bool IsThrowing => combat.state == PlayerCombat.PlayerState.AxeThrow;
    public bool IsCatching => catchTimer > 0f;

    private float throwTimer, catchTimer;
    private bool released;

    private AxeProjectile currentAxe;
    private PlayerMovement movement;
    private PlayerCombat combat;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
    }

    void OnThrowAxe(InputValue value)
    {
        if (!value.isPressed) return;
        if (currentAxe == null) StartThrow();
        else currentAxe.Recall();
    }

    void StartThrow()
    {
        if (combat.state != PlayerCombat.PlayerState.Normal) return;
        combat.state = PlayerCombat.PlayerState.AxeThrow;
        throwTimer = 0f;
        released = false;
    }

    void Update()
    {
        if (catchTimer > 0f) catchTimer -= Time.deltaTime;
        if (!IsThrowing) return;   // also true if a hit interrupted the throw

        throwTimer += Time.deltaTime;
        if (!released && throwTimer >= releaseTime) { released = true; SpawnAxe(); }
        if (throwTimer >= throwDuration) combat.state = PlayerCombat.PlayerState.Normal;
    }

    void SpawnAxe()
    {
        int facing = movement.FacingDirection;
        Vector2 pos = (Vector2)transform.position + new Vector2(spawnOffset.x * facing, spawnOffset.y);
        currentAxe = Instantiate(axePrefab, pos, Quaternion.identity);
        currentAxe.OnCaught += () => { currentAxe = null; catchTimer = catchDuration; };
        currentAxe.Launch(transform, facing);
    }

    public void RemoveAxe()
    {
        if (currentAxe != null) Destroy(currentAxe.gameObject);
        currentAxe = null;
        catchTimer = 0f;
    }
}