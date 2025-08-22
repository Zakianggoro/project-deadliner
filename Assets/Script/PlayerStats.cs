using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [Header("Energy Settings")]
    public Slider energyBar;   // drag dari UI
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float drainRate = 2f; // energi turun per detik

    void Start()
    {
        currentEnergy = maxEnergy;
        UpdateUI();
    }

    void Update()
    {
        // energy turun otomatis
        currentEnergy -= Time.deltaTime * drainRate;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateUI();

        if (currentEnergy <= 0)
        {
            Debug.Log("💤 Player tertidur! Game Over.");
        }
    }

    public void AddEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateUI();
    }

    void UpdateUI()
    {
        energyBar.value = currentEnergy / maxEnergy;
    }
}
