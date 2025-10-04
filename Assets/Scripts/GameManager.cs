using UnityEngine;

public class GameManager : MonoBehaviour
{
    public ObjectPool<Mine> pool;
    public Mine minePrefab;
    public int mineSize;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pool = new ObjectPool<Mine>(minePrefab);
        pool.Populate(mineSize);
    }

    // Update is called once per frame
    void Update()
    {
        var b = pool.GetPooledObject();

    }
}
