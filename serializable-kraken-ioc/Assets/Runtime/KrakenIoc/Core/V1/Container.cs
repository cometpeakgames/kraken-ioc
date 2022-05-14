using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CometPeak.ModularReferences;
using CometPeak.SerializableKrakenIoc.Interfaces;

namespace CometPeak.SerializableKrakenIoc {
    public delegate IContainer GlobalContainerFactoryHandler();

    /// <inheritdoc cref="IContainer"/>
    [Serializable]
    public class Container : IContainer, IDisposable, ISerializationCallbackReceiver {
        [SerializeField] private SerializableDictionary<SerializableType, BindingCollection> bindings = new Dictionary<SerializableType, BindingCollection>();
        [SerializeField] private SerializableDictionary<SerializableType, BindingCollection> clonedBindings = new Dictionary<SerializableType, BindingCollection>();
        [SerializeField] private SerializableDictionary<SerializableType, BindingCollection> inheritedBindings = new Dictionary<SerializableType, BindingCollection>();
        [SubclassSelector]
        [SerializeReference] private IInjector injector;

        //private Dictionary<Type, BindingCollection
        public LogHandler LogHandler { get; set; }
        private Dictionary<SerializableType, BindingCollection> Bindings => bindings;
        private Dictionary<SerializableType, BindingCollection> ClonedBindings => clonedBindings;
        private Dictionary<SerializableType, BindingCollection> InheritedBindings => inheritedBindings;

        public Container() { }

        public Container(IInjector injector) {
            Injector = injector;
        }

        public void OnBeforeSerialize() {
            Debug.LogWarning(bindings.Dictionary.Count);
        }
        public void OnAfterDeserialize() {
            Debug.LogWarning(bindings.Dictionary.Count);
        }

        public void Dispose() {
            var keys = Bindings.Keys;
            for (int i = keys.Count - 1; i >= 0; i--) {
                var key = keys.ElementAt(i);
                Dissolve(key);
            }

            var clonedKeys = ClonedBindings.Keys;
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
                if (injector == null) {
                    injector = new Injector(this);
                }

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

            if (!Bindings.ContainsKey(typeof(T)))
                Bindings.Add(typeof(T), new BindingCollection());

            Binding binding = new Binding {
                BinderTypes = new Type[] { typeof(T) },
                BoundType = type,
                Container = this
            };

            Bindings[typeof(T)].Add(binding);

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
                if (!Bindings.ContainsKey(interfaceType))
                    Bindings.Add(interfaceType, new BindingCollection());
                Bindings[interfaceType].Add(binding);
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
            if (Bindings.ContainsKey(type)) {
                IBinding binding = Bindings[type].GetBindingWithCategory(category);
                if (binding != null)
                    return binding;
            }

            if (ClonedBindings.ContainsKey(type)) {
                IBinding binding = ClonedBindings[type].GetBindingWithCategory(category);
                if (binding != null)
                    return binding;
            }

            if (InheritedBindings.ContainsKey(type)) {
                IBinding binding = InheritedBindings[type].GetBindingWithCategory(category);
                if (binding != null)
                    return binding;
            }

            return null;
        }

        public List<IBinding> GetBindings() {
            List<IBinding> bindings = new List<IBinding>();

            for (int i = 0; i < Bindings.Count; i++) {
                BindingCollection collection = Bindings.ElementAt(i).Value;
                bindings.AddRange(collection.GetBindings());
            }

            for (int i = 0; i < ClonedBindings.Count; i++) {
                BindingCollection collection = ClonedBindings.ElementAt(i).Value;
                bindings.AddRange(collection.GetBindings());
            }

            for (int i = 0; i < InheritedBindings.Count; i++) {
                BindingCollection collection = InheritedBindings.ElementAt(i).Value;
                bindings.AddRange(collection.GetBindings());
            }

            return bindings;
        }

        public List<Type> GetBindedTypes() {
            return Bindings.Keys.ToList().Select(s => s.Type).ToList();
        }

        /// <summary>
        /// Dissolves any bindings for type.
        /// </summary>
        /// <param name="type">Type.</param>
        public void Dissolve(Type type) {
            if (Bindings.ContainsKey(type)) {
                BindingCollection collection = Bindings[type];
                collection.Dissolve();
                Bindings.Remove(type);
            }

            if (ClonedBindings.ContainsKey(type)) {
                BindingCollection collection = ClonedBindings[type];
                collection.Dissolve();
                ClonedBindings.Remove(type);
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
            bindings.OnBeforeSerialize();
            bindings.OnAfterDeserialize();
            Debug.Log("Looking for " + (SerializableType) type + " in " + Bindings.Count + " bindings...\n\ncategory = " + category + "parentContext = \n" + parentContext);
            foreach (SerializableType key in Bindings.Keys) {
                Debug.Log("KEY " + key.Type.Name + " ====>\n\n" + (key == (SerializableType) type) + " vs. " + key.Equals(type) + " vs. " + key.Equals((SerializableType) type) + " vs. " + key.GetHashCode() + " vs. " + type.GetHashCode());
                Debug.Log(new Dictionary<Type, BindingCollection>() { { key, null } }.ContainsKey(type));
            }

            bool success = Bindings.ContainsKey(type);
            if (Bindings.ContainsKey(type)) {
                Debug.Log("???");
                IBinding binding = Bindings[type].GetBindingWithCategory(category);

                Debug.Log("???");
                if (binding != null) {
                    return binding.Resolve(parentContext);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }

            if (ClonedBindings.ContainsKey(type)) {
                IBinding binding = ClonedBindings[type].GetBindingWithCategory(category);

                if (binding != null) {
                    return binding.Resolve(parentContext);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }

            if (InheritedBindings.ContainsKey(type)) {
                IBinding binding = InheritedBindings[type].GetBindingWithCategory(category);

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
            if (Bindings.ContainsKey(type)) {
                IBinding binding = Bindings[type].GetBindingWithCategory(category);

                if (binding != null) {
                    return binding.Resolve(parentContext, target);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }

            if (ClonedBindings.ContainsKey(type)) {
                IBinding binding = ClonedBindings[type].GetBindingWithCategory(category);

                if (binding != null) {
                    return binding.Resolve(parentContext, target);
                } else {
                    throw new MissingBindingException(type, category);
                }
            }

            if (InheritedBindings.ContainsKey(type)) {
                IBinding binding = InheritedBindings[type].GetBindingWithCategory(category);

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
            return InheritedBindings.ContainsKey(type) || ClonedBindings.ContainsKey(type) || Bindings.ContainsKey(type);
        }

        public bool HasBindingForCategory<T>(object category) {
            return HasBindingForCategory(typeof(T), category);
        }

        public bool HasBindingForCategory(Type type, object category) {
            return (InheritedBindings.ContainsKey(type) && InheritedBindings[type].HasCategory(category)) ||
                (ClonedBindings.ContainsKey(type) && ClonedBindings[type].HasCategory(category)) ||
                (Bindings.ContainsKey(type) && Bindings[type].HasCategory(category));
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
                if (!InheritedBindings.ContainsKey(interfaceType))
                    InheritedBindings.Add(interfaceType, new BindingCollection());

                BindingCollection collection = InheritedBindings[interfaceType];

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
                if (!ClonedBindings.ContainsKey(interfaceType))
                    ClonedBindings.Add(interfaceType, new BindingCollection());

                BindingCollection collection = ClonedBindings[interfaceType];

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
