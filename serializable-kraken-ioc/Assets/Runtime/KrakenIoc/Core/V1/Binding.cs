using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CometPeak.ModularReferences;
using CometPeak.SerializableKrakenIoc.Interfaces;

namespace CometPeak.SerializableKrakenIoc {
    /// <inheritdoc cref="IBinding"/>
    [Serializable]
    public class Binding : IBinding {
        [SerializeReference] private static List<IBindingMiddleware> _bindingMiddleware = new List<IBindingMiddleware>();

        [SerializeField] private BindingType bindingType;
        [SerializeField] private SerializableType[] binderTypes;
        [SerializeField] private SerializableType boundType;
        [SerializeReference] private List<object> boundObjects;
        [SerializeField] private SerializableType factoryType;

        [SubclassSelector]
        [SerializeReference] private IBinding inheritedFromBinding;

        [SubclassSelector]
        [SerializeReference] private object category;

        /// <summary>
        /// Cached factory instance
        /// </summary>
        [SubclassSelector]
        [SerializeReference] private IFactory _cachedFactory = null;

        private Type[] binderTypesCache;


        public event Action<bool, IBinding, object> Resolved;

        /// <summary>
        /// Type of the binding - Transient vs Singleton
        /// </summary>
        public BindingType BindingType {
            get { return bindingType; }
            set { bindingType = value; }
        }

        /// <summary>
        /// Binder (contract/interface) types. 
        /// Array of interfaces bound to one implementation
        /// </summary>
        public Type[] BinderTypes {
            get {
                binderTypesCache = (binderTypes == null) ? null : binderTypes.Select(s => s.Type).ToArray();
                return binderTypesCache;
            }
            set {
                binderTypesCache = value;
                binderTypes = (binderTypesCache == null) ? null : binderTypesCache.Select(t => new SerializableType(t)).ToArray();
            }
        }

        /// <summary>
        /// Bound (concrete) type
        /// </summary>
        public Type BoundType {
            get { return boundType; }
            set { boundType = value; }
        }

        /// <summary>
        /// Bound objects - values to resolve
        /// </summary>
        public List<object> BoundObjects {
            get { return boundObjects; }
            set { boundObjects = value; }
        }

        /// <summary>
        /// Container
        /// </summary>
        public IContainer Container { get; set; }

        /// <summary>
        /// Factory type - used when object is created FromFactory
        /// </summary>
        public Type FactoryType {
            get { return factoryType; }
            set { factoryType = value; }
        }

        /// <summary>
        /// Factory method delegate - used when object is created FromFactoryMethod
        /// </summary>
        public FactoryMethod<object> FactoryMethod { get; set; }

        public object Category {
            get {
                return category;
            }
            set {
                if (category != null) {
                    throw new TypeCategoryAlreadyBoundException(BoundType, category);
                }

                category = value;
            }
        }

        public Binding() {
            BindingType = BindingType.Transient;
            BoundObjects = new List<object>();
        }

        public object ResolveWithMiddleware(object target = null) {
            return ResolveWithMiddleware(null, target);
        }

        public object ResolveWithMiddleware(IInjectContext injectContext, object target = null) {
            foreach (var middleware in _bindingMiddleware) {
                var result = middleware.Resolve(this, injectContext, target);

                if (result != null) {
                    return result;
                }
            }

            return null;
        }

        public T Resolve<T>(object target = null) {
            return (T) Resolve(target);
        }

        public object Resolve(object target = null) {
            return Resolve(null, target);
        }

        public object Resolve(IInjectContext parentContext, object target = null) {
            if (inheritedFromBinding != null) {
                return inheritedFromBinding.Resolve(parentContext, target);
            }

            // Attempt to resolve with middleware first...
            var result = ResolveWithMiddleware(parentContext, target);

            if (result != null) {
                return result;
            } else {
                return InternalResolve(parentContext);
            }
        }

        private object ResolveNew(IInjectContext injectContext) {
            object instance;

            if (FactoryMethod != null) {
                instance = FactoryMethod?.Invoke(injectContext);
            } else if (FactoryType != null) {
                if (_cachedFactory == null) {
                    _cachedFactory = (IFactory) Container.Resolve(FactoryType);
                }

                instance = _cachedFactory.Create(injectContext);
            } else {

                instance = Container.Injector.Resolve(BoundType, injectContext);
            }

            return instance;
        }

        private object InternalResolve(IInjectContext injectContext) {
            object instance;

            BoundObjects = BoundObjects ?? new List<object>();

            switch (BindingType) {
                case BindingType.Singleton:
                    if (BoundObjects.Count == 0) {
                        instance = ResolveNew(injectContext);
                        BoundObjects.Add(instance);

                        Container.Injector.Inject(instance, injectContext);

                        Resolved?.Invoke(true, this, null);
                    } else {
                        instance = BoundObjects.FirstOrDefault();

                        Resolved?.Invoke(false, this, null);
                    }
                    break;
                case BindingType.Transient:
                    instance = ResolveNew(injectContext);
                    Container.Injector.Inject(instance, injectContext);

                    Resolved?.Invoke(true, this, null);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            Debug.Log("RESOLVED " + instance + " (COUNT = " + BoundObjects.Count + ")");
            return instance;
        }

        /// <summary>
        /// Sets the lifetime scope of this binding to 'Singleton'.
        /// </summary>
        public void AsSingleton() {
            BindingType = BindingType.Singleton;
        }

        /// <summary>
        /// Sets the lifetime scope of this binding to 'Transient'. Returns
        /// a new instance every time it is resolved.
        /// </summary>
        public void AsTransient() {
            BindingType = BindingType.Transient;
        }

        /// <summary>
        /// Dissolves and removes any bound objects.
        /// </summary>
        public void Dissolve() {
            if (BoundObjects != null) {
                for (int i = BoundObjects.Count - 1; i >= 0; i--) {
                    IDisposable disposable = BoundObjects[i] as IDisposable;

                    if (disposable != null) {
                        disposable.Dispose();
                    }

                    BoundObjects[i] = null;
                    BoundObjects.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Assigns the boundType to the specifed type.
        /// </summary>
        /// <typeparam name="T">The concrete implementation type.</typeparam>
        public IBinding To<T>() {
            foreach (var interfaceType in BinderTypes) {
                if (!interfaceType.IsAssignableFrom(typeof(T))) {
                    throw new InvalidBindingException($"Can not bind ${interfaceType} type to type {typeof(T)}, {typeof(T)} does not implement {interfaceType}");
                }
            }

            BoundType = typeof(T);

            return this;
        }

        public void Inherit(IBinding binding) {
            BinderTypes = binding.BinderTypes;
            BindingType = binding.BindingType;
            BoundType = binding.BoundType;
            Category = binding.Category;
            FactoryType = binding.FactoryType;
            FactoryMethod = binding.FactoryMethod;


            inheritedFromBinding = binding;
        }

        public void CloneFrom(IBinding binding) {
            BinderTypes = binding.BinderTypes;
            BindingType = binding.BindingType;
            BoundType = binding.BoundType;
            Category = binding.Category;
            FactoryType = binding.FactoryType;
            FactoryMethod = binding.FactoryMethod;
        }

        public IBinding WithCategory(object category) {
            Category = category;

            return this;
        }

        public IBinding FromFactory<TFactory>() where TFactory : IFactory {
            if (FactoryMethod != null) {
                throw new InvalidBindingException($"Can not bind with FromFactory, binding already uses FromFactoryMethod");
            }

            FactoryType = typeof(TFactory);

            return this;
        }

        public IBinding FromFactory<TFactory, T>() where TFactory : IFactory<T> {
            foreach (var interfaceType in BinderTypes) {
                if (!interfaceType.IsAssignableFrom(typeof(T))) {
                    throw new InvalidBindingException($"Can not bind ${interfaceType} type to resolve from Factory of type ${typeof(TFactory)}");
                }
            }

            return FromFactory<TFactory>();
        }

        public IBinding FromFactoryMethod<T>(FactoryMethod<T> factoryMethod) {
            foreach (var interfaceType in BinderTypes) {
                if (!interfaceType.IsAssignableFrom(typeof(T))) {
                    throw new InvalidBindingException($"Can not bind ${interfaceType} type to resolve from Factory Method of type ${typeof(FactoryMethod<T>)}");
                }
            }

            if (FactoryType != null) {
                throw new InvalidBindingException($"Can not bind with FromFactoryMethod, binding already uses FromFactory");
            }

            if (factoryMethod == null) {
                throw new InvalidBindingException($"Can not bind FromFactoryMethod, factoryMethod is null");
            }

            FactoryMethod = delegate (IInjectContext injectionContext) {
                return factoryMethod.Invoke(injectionContext);
            };

            return this;
        }

        public void NotifyResolved(bool success, object value) {
            Resolved?.Invoke(success, this, value);
        }

        public static void AddBindingMiddleware(IBindingMiddleware bindingMiddleware) {
            _bindingMiddleware.Add(bindingMiddleware);
        }
    }

}
