using TMPro;
using UnityEngine;

public class UpgradeSlotController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentLevel;
    [SerializeField] private TextMeshProUGUI cost;
    [SerializeField] private int upgradeType;

    private void OnEnable() => UpdateData();

    public void UpdateData()
    {
        if (upgradeType == 0)
            SetupViscosity();
        if (upgradeType == 1)
            SetupSmoothness();
        if (upgradeType == 2)
            SetupMultiplier();
        if (upgradeType == 3)
            SetupSpeed();
    }

    // 1 - уже есть
    // 2 - 1
    // 3 - 3
    // costs - [1, 3]
    // 1 = costs[2-2]=[0]

    private void SetupViscosity()
    {
        currentLevel.text = PlayerStatic.ViscosityResistanceLevel.ToString();
        int costs = UpgradesController.GetCost(0);
        if (costs == -1)
            cost.text = "Max";
        else
            cost.text = costs.ToString();
    }

    private void SetupSmoothness()
    {
        
        currentLevel.text = PlayerStatic.SmoothnessResistanceLevel.ToString();
        int costs = UpgradesController.GetCost(1);
        if (costs == -1)
            cost.text = "Max";
        else
            cost.text = costs.ToString();
    }

    private void SetupMultiplier()
    {
        currentLevel.text = PlayerStatic.LootMultiplierLevel.ToString();
        int costs = UpgradesController.GetCost(2);
        if (costs == -1)
            cost.text = "Max";
        else
            cost.text = costs.ToString();
    }

    private void SetupSpeed()
    {
        currentLevel.text = PlayerStatic.SpeedLevel.ToString();
        int costs = UpgradesController.GetCost(3);
        if (costs == -1)
            cost.text = "Max";
        else
            cost.text = costs.ToString();
    }
}