using TMPro;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public float money;
    public float reputation;

    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI reputationText;

    public void Update()
    {
        moneyText.text = "Money: " + money.ToString("F2");
        reputationText.text = "Reputation: " + reputation.ToString("F2");
    }
}