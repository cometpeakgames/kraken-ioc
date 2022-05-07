using CometPeak.SerializableKrakenIoc.Interfaces;

namespace CometPeak.SerializableKrakenIoc
{
    public abstract class Factory<T> : IFactory<T>
    {
        public abstract T Create(IInjectContext injectionContext);

        object IFactory.Create(IInjectContext injectionContext)
        {
            return Create(injectionContext);
        }
    }
}
