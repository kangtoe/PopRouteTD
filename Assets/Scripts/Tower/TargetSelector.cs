using UnityEngine;

public static class TargetSelector
{
    public static Enemy SelectTarget(Vector3 towerPos, float range, TargetPriority priority, int enemyLayerMask)
    {
        var hits = Physics2D.OverlapCircleAll(towerPos, range, enemyLayerMask);
        if (hits.Length == 0) return null;

        Enemy best = null;
        float bestValue = float.MinValue;
        int bestLayer = int.MinValue;

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<Enemy>();
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            switch (priority)
            {
                case TargetPriority.First:
                {
                    float progress = enemy.Follower.Progress;
                    if (progress > bestValue)
                    {
                        bestValue = progress;
                        best = enemy;
                    }
                    break;
                }
                case TargetPriority.Close:
                {
                    float dist = -Vector3.Distance(towerPos, enemy.transform.position);
                    if (dist > bestValue)
                    {
                        bestValue = dist;
                        best = enemy;
                    }
                    break;
                }
                case TargetPriority.Last:
                {
                    float progress = enemy.Follower.Progress;
                    if (best == null || progress < bestValue)
                    {
                        bestValue = progress;
                        best = enemy;
                    }
                    break;
                }
                case TargetPriority.Strong:
                {
                    int layer = (int)enemy.CurrentLayer;
                    float progress = enemy.Follower.Progress;
                    if (layer > bestLayer || (layer == bestLayer && progress > bestValue))
                    {
                        bestLayer = layer;
                        bestValue = progress;
                        best = enemy;
                    }
                    break;
                }
            }
        }

        return best;
    }
}
