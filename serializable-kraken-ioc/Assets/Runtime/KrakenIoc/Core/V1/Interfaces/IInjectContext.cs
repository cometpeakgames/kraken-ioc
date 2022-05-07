using System;

namespace CometPeak.SerializableKrakenIoc.Interfaces {
    public interface IInjectContext {
        /// <summary>
        /// Container being used for an injection
        /// </summary>
        IContainer Container { get; }

        /// <summary>
        /// Type that 
        /// </summary>
        Type DeclaringType { get; }

        /// <summary>
        /// Parent context
        /// </summary>
        IInjectContext ParentContext { get; }
    }
}
