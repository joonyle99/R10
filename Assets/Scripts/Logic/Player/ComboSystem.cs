public class ComboSystem
{
    public int Count { get; private set; }

    public void Add() => Count++;
    public void Reset() => Count = 0;
}
