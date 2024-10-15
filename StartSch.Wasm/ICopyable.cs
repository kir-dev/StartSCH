namespace StartSch.Wasm;

public interface ICopyable<out TThis>
{
    TThis Copy();
}