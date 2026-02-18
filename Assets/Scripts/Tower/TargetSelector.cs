using UnityEngine;

public static class TargetSelector
{
    public static Enemy SelectTarget(Vector3 towerPos, float range, TargetPriority priority, int enemyLayerMask)
    {
        var hits = Physics2D.OverlapCircleAll(towerPos, range, enemyLayerMask);
        if (hits.Length == 0) return null;

        Enemy best = null;
        float bestValue = float.MinValue;

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<Enemy>();
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            float value = priority switch
            {
                TargetPriority.First => enemy.Follower.Progress,
                TargetPriority.Close => -Vector3.Distance(towerPos, enemy.transform.position),
                TargetPriority.Weak => -(int)enemy.CurrentLayer,
                TargetPriority.Strong => (int)enemy.CurrentLayer,
                _ => 0f
            };

            if (value > bestValue)
            {
                bestValue = value;
                best = enemy;
            }
        }

        return best;
    }
}
