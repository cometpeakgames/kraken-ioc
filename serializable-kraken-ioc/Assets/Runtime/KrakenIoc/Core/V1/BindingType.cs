﻿namespace CometPeak.SerializableKrakenIoc
{
    public enum BindingType
    {
        /// <summary>
        /// A single-instance / singleton of a type.
        /// </summary>
        Singleton,
        /// <summary>
        /// Numerous instances of a type.
        /// </summary>
        Transient,
    }

}