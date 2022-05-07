﻿using System;
using System.Reflection;

namespace CometPeak.SerializableKrakenIoc
{
    /// <summary>
    /// Internally cached representation of the parameter and injected attribute
    /// </summary>
    public struct ParameterInfoCache
    {
        public ParameterInfo ParameterInfo { get; set; }
        public InjectAttribute InjectAttribute { get; set; }

        public ParameterInfoCache(ParameterInfo parameterInfo, InjectAttribute injectAttribute)
        {
            ParameterInfo = parameterInfo;
            InjectAttribute = injectAttribute;
        }
    }
}
