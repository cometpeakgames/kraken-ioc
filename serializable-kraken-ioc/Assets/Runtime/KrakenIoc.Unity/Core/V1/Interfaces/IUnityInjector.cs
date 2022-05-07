﻿using UnityEngine;

namespace CometPeak.SerializableKrakenIoc.Interfaces
{
    public interface IUnityInjector
    {
        /// <summary>
        /// Injects values into the components on the gameObject.
        /// Can optionally recurse the children.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="recurseChildren"></param>
        void InjectGameObject(GameObject gameObject, bool recurseChildren = false);
    }
}
