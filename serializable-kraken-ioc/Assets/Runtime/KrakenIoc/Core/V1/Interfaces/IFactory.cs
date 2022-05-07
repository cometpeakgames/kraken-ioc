namespace CometPeak.SerializableKrakenIoc.Interfaces {
    public interface IFactory {
        object Create(IInjectContext injectionContext);
    }

    public interface IFactory<T> : IFactory {
        new T Create(IInjectContext injectionContext);
    }
}
