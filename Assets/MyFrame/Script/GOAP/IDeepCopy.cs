public interface IDeepCopy<T> where T : IDeepCopy<T>
{
   public T DeepCopy();
}
