using UnityEngine;
using UnityEngine.UI;

public class PowerController : MonoBehaviour
{
    [SerializeField] private Vector2 powerDecrease;
    [SerializeField] private Image powerUI;
    [SerializeField] private GameObject diePanel;

    private float power;

    private void Start()
    {
        power = 1;
        powerUI.fillAmount = 1;
    }

    private void Update()
    {
        power = Mathf.Clamp01(power - (powerDecrease.x + powerDecrease.y / PlayerStatic.EngineLevel) * Time.deltaTime);
        powerUI.fillAmount = power;
        if (power == 0)
        {
            Time.timeScale = 0;
            diePanel?.SetActive(true);
        }
    }
}
