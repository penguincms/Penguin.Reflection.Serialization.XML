﻿using System;

namespace Penguin.Reflection.Serialization.XML
{
    /// <summary>
    /// Used to set the XML Deserializer options
    /// </summary>
    public class XMLDeserializerOptions
    {
        /// <summary>
        /// If true, XML node attributes should be mapped to properties with corresponding names
        /// Default: true
        /// </summary>
        public bool AttributesAsProperties { get; set; } = true;

        /// <summary>
        /// Nodes => Deserialization case sensitivity
        /// Default: true
        /// </summary>
        public bool CaseSensitive { get; set; } = true;

        /// <summary>
        /// If the target object is a child of the XML, naming the node allows the deserializer to slice out just that child
        /// </summary>
        public string StartNode { get; set; }

        internal StringComparison NameComparisonType => this.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }
}