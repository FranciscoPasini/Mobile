using UnityEngine;

public interface IPoolable
{
    void Spawn(Vector3 position);
    void Despawn();
}
