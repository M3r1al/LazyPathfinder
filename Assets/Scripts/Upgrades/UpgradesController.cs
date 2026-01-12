using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class UpgradesController : MonoBehaviour
{
    private static UpgradesController instance;
    [SerializeField] private GameObject upgradesPanel;
    [SerializeField] private TextMeshProUGUI money;
    [SerializeField] private TextMeshProUGUI advice;
    [SerializeField] private int[] viscosityCost;
    [SerializeField] private int[] smoothnessCost;
    [SerializeField] private int[] multiplierCost;
    [SerializeField] private int[] speedCosts;
    [SerializeField] private UpgradeSlotController[] slots;
    private readonly string[] names = new string[4] {"Viscosity Resistance", "Smoothness Resistance", "Loot Multiplier", "Speed"};

    private bool isOpened = false;
    private PlayerInput playerInput;
    private InputAction clickAction;

    private void Awake()
    {
        instance = this;
        playerInput = GetComponent<PlayerInput>();
        clickAction = playerInput.actions["Upgrades"];
    }

    public void FindBestUpgrade()
    {
        int cheepestValue = GetCost(0);
        if (cheepestValue < 0)
            cheepestValue = 1000000000;
        int cheepestIndex = 0;
        
        if (cheepestValue > GetCost(1) && GetCost(1) > 0)
        {
            cheepestValue = GetCost(1);
            cheepestIndex = 1;
        }
        if (cheepestValue > GetCost(2) && GetCost(2) > 0)
        {
            cheepestValue = GetCost(2);
            cheepestIndex = 2;
        }
        if (cheepestValue > GetCost(3) && GetCost(3) > 0)
        {
            cheepestValue = GetCost(3);
            cheepestIndex = 3;
        }

        if (PlayerStatic.Money >= cheepestValue)
            advice.text = names[cheepestIndex];
        else
            advice.text = "Collect more";
    }

    private void Start()
    {
        upgradesPanel.SetActive(false);
    }

    private void OnEnable()
    {
        clickAction.performed += OnButtonPressed;
    }

    private void OnDisable()
    {
        clickAction.performed -= OnButtonPressed;
    }

    private void OnButtonPressed(InputAction.CallbackContext ctx)
    {
        isOpened = !isOpened;
        upgradesPanel.SetActive(isOpened);
        money.text = PlayerStatic.Money.ToString();
        FindBestUpgrade();
        GemsFinder.ChangePlayerState(!isOpened);
    }

    // Viscosity Resistance - 0
    // Smoothness Resistance - 1
    // Loot Multiplier - 2
    // Speed - 3
    public void UpgradePlayer(int type)
    {
        if (type == 0 && PlayerStatic.Money >= viscosityCost[PlayerStatic.ViscosityResistanceLevel - 1])
        {
            Buy(viscosityCost[PlayerStatic.ViscosityResistanceLevel - 1]);
            PlayerStatic.ViscosityResistanceLevel++;
        }
        if (type == 1 && PlayerStatic.Money >= smoothnessCost[PlayerStatic.SmoothnessResistanceLevel - 1])
        {
            Buy(smoothnessCost[PlayerStatic.SmoothnessResistanceLevel - 1]);
            PlayerStatic.SmoothnessResistanceLevel++;
        }
        if (type == 2 && PlayerStatic.Money >= multiplierCost[PlayerStatic.LootMultiplierLevel - 1])
        {
            Buy(smoothnessCost[PlayerStatic.LootMultiplierLevel - 1]);
            PlayerStatic.LootMultiplierLevel++;
        }
        if (type == 3 && PlayerStatic.Money >= speedCosts[PlayerStatic.SpeedLevel - 1])
        {
            Buy(speedCosts[PlayerStatic.SpeedLevel - 1]);
            PlayerStatic.SpeedLevel++;
        }
        slots[type].UpdateData();
        FindBestUpgrade();
    }

    private void Buy(int cost)
    {
        PlayerStatic.Money -= cost;
        money.text = PlayerStatic.Money.ToString();
    }

    public static int GetCost(int type)
    {
        if (instance == null)
            instance = FindAnyObjectByType<UpgradesController>();
        
        if (type == 0 && PlayerStatic.ViscosityResistanceLevel <= instance.viscosityCost.Length)
            return instance.viscosityCost[PlayerStatic.ViscosityResistanceLevel - 1];
        if (type == 1 && PlayerStatic.SmoothnessResistanceLevel <= instance.smoothnessCost.Length) 
            return instance.smoothnessCost[PlayerStatic.SmoothnessResistanceLevel - 1];
        if (type == 2 && PlayerStatic.LootMultiplierLevel <= instance.multiplierCost.Length)
            return instance.multiplierCost[PlayerStatic.LootMultiplierLevel - 1];
        if (type == 3 && PlayerStatic.SpeedLevel <= instance.speedCosts.Length)
            return instance.speedCosts[PlayerStatic.SpeedLevel - 1];
        return -1;
    }
}