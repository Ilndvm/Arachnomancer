public interface IPoolable
{
    bool IsActive { get; }


    void Disable();

    void Reset();
}