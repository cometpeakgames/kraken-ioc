using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CometPeak.SerializableKrakenIoc.Interfaces;

namespace CometPeak.SerializableKrakenIoc {
    /// <summary>
    /// Contains information about bindings and categories
    /// </summary>
    [Serializable]
    internal class BindingCollection {
        [SerializeReference] private List<IBinding> bindings = new List<IBinding>();

        /// <summary>
        /// This cache is used to lookup a binding by category in ~O(n)
        /// </summary>
        private Dictionary<object, IBinding> categoryCache = new Dictionary<object, IBinding>();

        /// <summary>
        /// Binding with no category - cached
        /// </summary>
        private IBinding defaultBindingCached = null;

        public void Add(IBinding binding) {
            bindings.Add(binding);
        }

        public void Remove(IBinding binding) {
            // Invalidate binding in the cache
            if (binding.Category == null) {
                if (defaultBindingCached == binding) {
                    // Remove default binding from the cache
                    defaultBindingCached = null;
                }

                // Check that we don't have non-cached bindings cached with a default category (Remove() called before any Resolve())
                foreach (var kvp in categoryCache.Where(kvp => kvp.Value.Category == null).ToList()) {
                    categoryCache.Remove(kvp.Key);
                }
            } else {
                if (categoryCache.ContainsKey(binding.Category)) {
                    // Remove cached category
                    categoryCache.Remove(binding.Category);
                }

                // Check that we don't have non-cached binding with that category
                foreach (var kvp in categoryCache.Where(kvp => kvp.Value.Category != null && kvp.Value.Category.Equals(binding.Category)).ToList()) {
                    categoryCache.Remove(kvp.Key);
                }
            }

            bindings.Remove(binding);
        }

        public bool HasCategory(object category) {
            return GetBindingWithCategory(category) != null;
        }

        public IBinding GetBindingWithCategory(object category) {
            // Default category (no category)
            if (category == null) {
                return GetDefaultBinding();
            }

            IBinding binding;

            // Check if category cache contains this binding
            if (categoryCache.ContainsKey(category)) {
                binding = categoryCache[category];

                if (category.Equals(binding.Category)) {
                    return binding;
                } else {
                    // Category was changed i.e. using WithCategory(), invalidate the cache
                    categoryCache[binding.Category] = binding;
                }
            }

            // Category cache does not contain this binding, OR it contained the wrong one and it was invalidated. Run a lookup and cache it.
            binding = bindings.FirstOrDefault(b => category.Equals(b.Category));

            if (binding != null) {
                categoryCache[category] = binding;
            }

            return binding;

        }

        public IEnumerable<IBinding> GetBindings() {
            return bindings;
        }

        public void Dissolve() {
            foreach (var binding in bindings) {
                binding.Dissolve();
            }

            bindings.Clear();
        }

        private IBinding GetDefaultBinding() {
            // Check default binding cache
            if (defaultBindingCached == null) {
                // No default binding cache - run the lookup
                IBinding binding = bindings.FirstOrDefault(b => b.Category == null);

                if (binding == null) {
                    return null;
                } else {
                    defaultBindingCached = binding; // Cache
                    return binding;
                }
            } else {
                // Check if default binding has category changed and needs to be invalidated
                if (defaultBindingCached.Category != null) {
                    categoryCache[defaultBindingCached.Category] = defaultBindingCached;
                    defaultBindingCached = bindings.FirstOrDefault(b => b.Category == null);
                }

                return defaultBindingCached;
            }
        }
    }
}
