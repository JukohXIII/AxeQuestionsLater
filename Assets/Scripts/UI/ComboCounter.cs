using TMPro;
using UnityEngine;

public class ComboCounter : MonoBehaviour
{
    [SerializeField] private GameObject comboRoot;   // "Combo": parent of both texts
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private float resetTime = 2f;
    [SerializeField] private int minHitsToShow = 2;

    [Header("Ranks")]
    [SerializeField] private string[] ranks = { "D", "C", "B", "A", "S" };
    [SerializeField] private int[] rankHits = { 5, 10, 20, 35, 50 };

    private static ComboCounter instance;
    private int count;
    private float timer;

    void Awake()
    {
        instance = this;
        comboRoot.SetActive(false);
    }

    public static void AddHit() { if (instance != null) instance.Add(); }
    public static void Break()  { if (instance != null) instance.ResetCombo(); }

    void Add()
    {
        count++;
        timer = resetTime;
        if (count >= minHitsToShow)
        {
            comboRoot.SetActive(true);
            rankText.text = Rank();
            comboText.text = count + " hits";
        }
    }

    void Update()
    {
        if (count == 0) return;
        timer -= Time.deltaTime;   // pauses during hitstop and the pause menu
        if (timer <= 0f) ResetCombo();
    }

    void ResetCombo()
    {
        count = 0;
        comboRoot.SetActive(false);
    }

    string Rank()
    {
        string rank = "";   // no letter until the first rank
        for (int i = 0; i < rankHits.Length; i++)
            if (count >= rankHits[i]) rank = ranks[i];
        return rank;
    }
}