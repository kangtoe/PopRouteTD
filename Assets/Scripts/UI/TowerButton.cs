using UnityEngine;
using UnityEngine.UI;

public class TowerButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text labelText;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Text cooldownText;
    [SerializeField] private Image iconImage;

    public Button Button => button;
    public Text LabelText => labelText;
    public Image CooldownOverlay => cooldownOverlay;
    public Text CooldownText => cooldownText;
    public Image IconImage => iconImage;
}
