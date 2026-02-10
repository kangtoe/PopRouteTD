using UnityEngine;

public static class TargetSelector
{
    public static Balloon SelectTarget(Vector3 towerPos, float range, TargetPriority priority, int enemyLayerMask)
    {
        var hits = Physics2D.OverlapCircleAll(towerPos, range, enemyLayerMask);
        if (hits.Length == 0) return null;

        Balloon best = null;
        float bestValue = priority == TargetPriority.Close || priority == TargetPriority.Weak
            ? float.MaxValue
            : float.MinValue;

        foreach (var hit in hits)
        {
            var balloon = hit.GetComponent<Balloon>();
            if (balloon == null || !balloon.gameObject.activeInHierarchy) continue;

            float value = priority switch
            {
                TargetPriority.First => balloon.Follower.Progress,
                TargetPriority.Close => -Vector3.Distance(towerPos, balloon.transform.position),
                TargetPriority.Weak => -(int)balloon.CurrentLayer,
                TargetPriority.Strong => (int)balloon.CurrentLayer,
                _ => 0f
            };

            if (value > bestValue)
            {
                bestValue = value;
                best = balloon;
            }
        }

        return best;
    }
}
