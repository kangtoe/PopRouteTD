using UnityEngine;

[CreateAssetMenu(fileName = "EnemyLayerColors", menuName = "Data/Enemy Layer Colors")]
public class EnemyLayerColors : ScriptableObject
{
    [SerializeField] private Color red = Color.red;
    [SerializeField] private Color orange = new(1f, 0.5f, 0f);
    [SerializeField] private Color yellow = Color.yellow;
    [SerializeField] private Color green = Color.green;
    [SerializeField] private Color blue = Color.blue;
    [SerializeField] private Color indigo = new(0.29f, 0f, 0.51f);
    [SerializeField] private Color purple = new(0.58f, 0f, 0.83f);

    public Color GetColor(EnemyLayer layer)
    {
        return layer switch
        {
            EnemyLayer.Red => red,
            EnemyLayer.Orange => orange,
            EnemyLayer.Yellow => yellow,
            EnemyLayer.Green => green,
            EnemyLayer.Blue => blue,
            EnemyLayer.Indigo => indigo,
            EnemyLayer.Purple => purple,
            _ => Color.white
        };
    }
}
