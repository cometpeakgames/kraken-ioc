using System;
using System.Collections.Generic;
using System.Linq;
using CometPeak.SerializableKrakenIoc.Interfaces;

namespace CometPeak.SerializableKrakenIoc {
    public delegate IContainer GlobalContainerFactoryHandler();

    /// <inheritdoc cref="IContainer"/>
    public class Container : IContainer, IDisposable {
        private Dictionary<Type, BindingCollection> bindings = new Dictionary<Type, BindingCollection>();
        private Dictionary<Type, BindingCollection> clonedBindings = new Dictionary<Type, BindingCollection>();
        private readonly Dictionary<Type, BindingCollection> inheritedBindings = new Dictionary<Type, BindingCollection>();
        private IInjector injector;

        public LogHandler LogHandler { get; set; }

        public Container() { }

        public Container(IInjector injector) {
            Injector = injector;
        }

        public void Dispose() {
            var keys = bindings.Keys;
            for (int i = keys.Count - 1; i >= 0; i--) {
                var key = keys.ElementAt(i);
                Dissolve(key);
            }

            var clonedKeys = clonedBindings.Keys;
            for (int i = clonedKeys.Count - 1; i >= 0; i--) {
                var key = clonedKeys.ElementAt(i);
                Dissolve(key);
            }

            bindings = null;
            clonedBindings = null;
        }

        public bool ShouldLog { get; set; }

        public IInjector Injector {
            get {
                if (injector == null)
                    injector = new Injector(this);
                return injector;
            }
            set {
                injector = value;
            }
        }

        public IBinding Bind<T>() {
            return Bind<T>(typeof(T));
        }

        public IBinding Bind<TInterface, TImplementation>() where TImplementation : TInterface {
            return Bind<TInterface>().To<TImplementation>();
        }

        public IBinding Bind<T>(object category) {
            return Bind<T>().WithCategory(category);
        }

        public IBinding Bind<T>(T value) {
            IBinding binding = Bind<T>(typeof(T));
            binding.BoundObjects.Add(value);
            binding.AsSingleton();

            return binding;
        }

        public IBinding Bind<T>(Type type) {
            if (!typeof(T).IsAssignableFrom(type))
                throw new InvalidBindingException($"Can not bind ${typeof(T)} type to type {type}, {type} does not implement {typeof(T)}");

            if (!bindings.ContainsKey(typeof(T)))
                bindings.Add(typeof(T), new BindingCollection());

            Binding binding = new Binding {
                BinderTypes = new Type[] { typeof(T) },
                BoundType = type,
                Container = this
            };

            bindings[typeof(T)].Add(binding);

            return binding;
        }

        public IBinding Bind(params Type[] interfaceTypes) {
            if (interfaceTypes == null)
                throw new InvalidBindingException($"Can not bind multiple interfaces, provided array of interface types is null");

            if (interfaceTypes.Length == 0)
                throw new InvalidBindingException($"Can not bind multiple interfaces, provided array of interface types is empty");

            Binding binding = new Binding {
                BinderTypes = interfaceTypes,
                BoundType = interfaceTypes[0], // By default, bind to a first interface type. Override using To<T> syntax
                Container = this
            };

            foreach (var interfaceType in interfaceTypes) {
                if (!bindings.ContainsKey(interfaceType))
                    bindings.Add(interfaceType, new BindingCollection());
                bindings[interfaceType].Add(binding);
            }

            return binding;
        }

        private void LogError(string format, params object[] args) {
            if (!ShouldLog)
                return;
            LogHandler?.Invoke(format, args);
        }

        public IBinding GetBinding<T>(object category) {
            return GetBinding(typeof(T), category);
        }

        public IBinding GetBinding<T>() {
            return GetBinding(typeof(T), null);
        }

        public IBinding GetBinding(Type type) {
            return GetBinding(type, null);
        }

        public IBinding GetBinding(Type type, object category) {
            if (bindings.ContainsKey(type)) {
                IBinding binding = bindings[type].GetBindingWithCategory(category);
                if (binding != null)
                    return binding;
            }

            if (clonedBindings.ContainsKey(type)) {
                IBinding binding = clonedBindings[type].GetBindingWithCategory(category);
                if (binding != null)
                    return binding;
            }

            if (inheritedBindings.ContainsKey(type)) {
                IBinding binding = inheritedBindings[type].GetBindingWithCategory(category);
                if (binding != null)
                    return binding;
            }

            return null;
        }

        public List<IBinding> GetBindings() {
            List<IBinding> bindings = new List<IBinding>();

            for (int i = 0; i < bindings.Count; i++) {
                BindingCollection collection = this.bindings.ElementAt(i).Value;
                bindings.AddRange(collection.GetBindings());
            }

            for (int i = 0; i < clonedBindings.Count; i++) {
                BindingCollection collection = clonedBindings.ElementAt(i).Value;
                bindings.AddRange(collection.GetBindings());
            }

            for (int i = 0; i < inheritedBindings.Count; i++) {
                BindingCollection collection = inheritedBindings.ElementAt(i).Value;
                bindings.AddRange(collection.GetBindings());
            }

            return bindings;
        }

        public List<Type> GetBindedTypes() {
            return bindings.Keys.ToList();
        }

        /// <summary>
        /// Dissolves any bindings for type.
        /// </summary>
        /// <param name="type">Type.</param>
        public void Dissolve(Type type) {
            if (bindings.ContainsKey(type)) {
                BindingCollection collection = bindings[type];
                collection.Dissolve();
                bindings.Remove(type);
            }

            if (clonedBindings.ContainsKey(type)) {
                BindingCollection collection = clonedBindings[type];
                collection.Dissolve();
                clonedBindings.Remove(type);
            }
        }

        /// <summary>
        /// Dissolves any bindings for type.
        /// </summary>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public void Dissolve<T>() {
            Dissolve(typeof(T));
        }

        public T Resolve<T>() {
            return (T) Resolve(typeof(T));
        }

        public T Resolve<T>(object target) {
            return (T) Resolve(typeof(T), target);
        }

        public T Resolve<T>(string category) {
            return ResolveWithCategory<T>(category);
        }

        public object Resolve(Type type) {
            return Resolve(type, null);
        }

        public object Resolve(Type type, IInjectContext parentContext) {
            return ResolveWithCategory(type, null, parentContext);
        }

        public object Resolve(Type type, object target) {
            return ResolveWithCategory(type, target, null, null);
        }

        public T ResolveWithCategory<T>(object category) {
            return (T) ResolveWithCategory(typeof(T), category);
        }

        public T ResolveWithCategory<T>(object target, object category) {
            return (T) ResolveWithCategory(typeof(T), target, category);
        }

        public object ResolveWithCategory(Type type, object category) {
            return ResolveWithCategory(type, category, null);
        }

        public object ResolveWithCategory(Type type, object category, IInjectContext parentContext) {
            if (bindings.ContainsKey(type)) {
                IBinding binding = bindings[type].GetBindingWithCategory(category);

                if (binding != null) {
                    return binding.Resolve(parentContext);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }

            if (clonedBindings.ContainsKey(type)) {
                IBinding binding = clonedBindings[type].GetBindingWithCategory(category);

                if (binding != null) {
                    return binding.Resolve(parentContext);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }

            if (inheritedBindings.ContainsKey(type)) {
                IBinding binding = inheritedBindings[type].GetBindingWithCategory(category);

                if (binding != null) {
                    return binding.Resolve(parentContext);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }


            throw new MissingBindingException(type, category);
        }

        public object ResolveWithCategory(Type type, object target, object category) {
            return ResolveWithCategory(type, target, category, null);
        }

        public object ResolveWithCategory(Type type, object target, object category, IInjectContext parentContext) {
            if (bindings.ContainsKey(type)) {
                IBinding binding = bindings[type].GetBindingWithCategory(category);

                if (binding != null) {
                    return binding.Resolve(parentContext, target);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }

            if (clonedBindings.ContainsKey(type)) {
                IBinding binding = clonedBindings[type].GetBindingWithCategory(category);

                if (binding != null) {
                    return binding.Resolve(parentContext, target);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }

            if (inheritedBindings.ContainsKey(type)) {
                IBinding binding = inheritedBindings[type].GetBindingWithCategory(category);

                if (binding != null) {
                    return binding.Resolve(parentContext, target);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }

            throw new MissingBindingException(type, category);
        }

        public bool HasBindingFor<T>() {
            return HasBindingFor(typeof(T));
        }

        public bool HasBindingFor(Type type) {
            return inheritedBindings.ContainsKey(type) || clonedBindings.ContainsKey(type) || bindings.ContainsKey(type);
        }

        public bool HasBindingForCategory<T>(object category) {
            return HasBindingForCategory(typeof(T), category);
        }

        public bool HasBindingForCategory(Type type, object category) {
            return (inheritedBindings.ContainsKey(type) && inheritedBindings[type].HasCategory(category)) ||
                (clonedBindings.ContainsKey(type) && clonedBindings[type].HasCategory(category)) ||
                (bindings.ContainsKey(type) && bindings[type].HasCategory(category));
        }

        public void Inherit(IContainer container) {
            List<IBinding> bindings = container.GetBindings();

            for (int i = 0; i < bindings.Count; i++) {
                IBinding binding = bindings[i];

                if (binding.BindingType == BindingType.Singleton) {
                    Binding inheritedBinding = new Binding {
                        Container = this
                    };

                    inheritedBinding.Inherit(binding);
                    AddInheritedBinding(inheritedBinding);
                } else {
                    // Clone transient bindings, allowing to resolve from this, inherited container (instead of parent)
                    Binding clonedBinding = new Binding() {
                        Container = this
                    };
                    clonedBinding.CloneFrom(binding);
                    AddClonedBinding(clonedBinding);
                }
            }
        }

        private void AddInheritedBinding(Binding binding) {
            foreach (Type interfaceType in binding.BinderTypes) {
                if (!inheritedBindings.ContainsKey(interfaceType))
                    inheritedBindings.Add(interfaceType, new BindingCollection());

                BindingCollection collection = inheritedBindings[interfaceType];

                IBinding existingBinding = collection.GetBindingWithCategory(binding.Category);

                if (existingBinding == null) {
                    collection.Add(binding);
                } else {
                    if (binding.Category == null) {
                        throw new TypeAlreadyBoundException(interfaceType);
                    } else {
                        throw new TypeCategoryAlreadyBoundException(interfaceType, binding.Category);
                    }
                }
            }
        }

        private void AddClonedBinding(Binding binding) {
            foreach (Type interfaceType in binding.BinderTypes) {
                if (!clonedBindings.ContainsKey(interfaceType))
                    clonedBindings.Add(interfaceType, new BindingCollection());

                BindingCollection collection = clonedBindings[interfaceType];

                IBinding existingBinding = collection.GetBindingWithCategory(binding.Category);

                if (existingBinding == null) {
                    collection.Add(binding);
                } else {
                    if (binding.Category == null) {
                        throw new TypeAlreadyBoundException(interfaceType);
                    } else {
                        throw new TypeCategoryAlreadyBoundException(interfaceType, binding.Category);
                    }
                }
            }
        }

        public void Bootstrap<T>() where T : IBootstrap {
            Bootstrap(typeof(T));
        }

        public void Bootstrap(Type type) {
            IBootstrap bootstrap = (IBootstrap) Activator.CreateInstance(type);
            bootstrap?.SetupBindings(this);
        }
    }
}
