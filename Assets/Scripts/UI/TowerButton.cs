using UnityEngine;
using UnityEngine.UI;

public class TowerButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text labelText;
    [SerializeField] private Image iconImage;

    public Button Button => button;
    public Text LabelText => labelText;
    public Image IconImage => iconImage;
}
