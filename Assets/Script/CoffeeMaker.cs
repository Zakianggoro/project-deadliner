using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CoffeeMaker : MonoBehaviour
{
    [Header("Progress Bar")]
    public Slider progressBar;      // drag dari UI
    public PlayerStats playerStats; // drag PlayerStats
    public float energyGain = 50f;  // berapa banyak energi naik

    private int step = 0;           // 0=air, 1=kopi, 2=gula, 3=mesin
    private bool isMaking = false;

    void Start()
    {
        progressBar.gameObject.SetActive(false); // sembunyikan progress bar awal
    }

    // fungsi ini dipanggil tombol UI (misalnya button Air, Kopi, Gula, Mesin)
    public void PressStep(int inputStep)
    {
        if (!isMaking)
        {
            switch (inputStep)
            {
                case 0:
                    Debug.Log("Start step 0: Air (8s)");
                    StartCoroutine(DoStep(0, 8f));
                    break;
                case 1:
                    Debug.Log("Start step 1: Kopi (3s)");
                    StartCoroutine(DoStep(1, 3f));
                    break;  // Kopi
                case 2:
                    Debug.Log("Start step 2: Gula (0.5s)");
                    StartCoroutine(DoStep(2, 0.5f));
                    break; // Gula
                case 3:
                    Debug.Log("Start step 3: Mesin (5s)");
                    StartCoroutine(DoStep(3, 5f));
                    break;  // Mesin
            }
        }
    }

    IEnumerator DoStep(int inputStep, float duration)
    {
        if (inputStep != step) // cek urutan
        {
            Debug.Log("❌ Urutan salah! Kopi gagal.");
            ResetCoffee();
            yield break;
        }

        isMaking = true;
        progressBar.gameObject.SetActive(true);
        progressBar.value = 0;

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            progressBar.value = t / duration;
            yield return null;
        }

        progressBar.gameObject.SetActive(false);
        isMaking = false;
        step++;

        if (step > 3) CoffeeDone();
    }

    void CoffeeDone()
    {
        Debug.Log("☕ Kopi jadi! Energy +" + energyGain);
        playerStats.AddEnergy(energyGain);
        ResetCoffee();
    }

    void ResetCoffee()
    {
        step = 0;
        isMaking = false;
        progressBar.gameObject.SetActive(false);
    }
}
