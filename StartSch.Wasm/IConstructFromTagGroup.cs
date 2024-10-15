namespace StartSch.Wasm;

public interface IConstructFromTagGroup<out TThis, TData>
    where TThis : TagGroup<TThis, TData>, IConstructFromTagGroup<TThis, TData>
{
    static abstract TThis ConstructFrom<TSource>(TSource source) where TSource : TagGroup<TSource, TData>, IConstructFromTagGroup<TSource, TData>;
}