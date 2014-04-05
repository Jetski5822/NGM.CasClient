﻿/*
 * Licensed to Jasig under one or more contributor license
 * agreements. See the NOTICE file distributed with this work
 * for additional information regarding copyright ownership.
 * Jasig licenses this file to you under the Apache License,
 * Version 2.0 (the "License"); you may not use this file
 * except in compliance with the License. You may obtain a
 * copy of the License at:
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on
 * an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied. See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using NGM.CasClient.Client.Validation.Schema.XmlDsig;

#pragma warning disable 1591

namespace NGM.CasClient.Client.Validation.Schema.Saml20.Metadata
{
    [Serializable]
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [XmlType(Namespace="urn:oasis:names:tc:SAML:2.0:metadata")]
    [XmlRoot("EntitiesDescriptor", Namespace="urn:oasis:names:tc:SAML:2.0:metadata", IsNullable=false)]
    public class EntitiesDescriptorType {
        [XmlElement(Namespace="http://www.w3.org/2000/09/xmldsig#")]
        public SignatureType Signature
        {
            get;
            set;
        }

        public ExtensionsType Extensions
        {
            get;
            set;
        }

        [XmlElement("EntitiesDescriptor", typeof(EntitiesDescriptorType))]
        [XmlElement("EntityDescriptor", typeof(EntityDescriptorType))]
        public object[] Items
        {
            get;
            set;
        }

        [XmlAttribute("validUntil")]
        public DateTime ValidUntil
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool ValidUntilSpecified
        {
            get;
            set;
        }

        [XmlAttribute("cacheDuration",DataType="duration")]
        public string CacheDuration
        {
            get;
            set;
        }

        [XmlAttribute("ID",DataType="ID")]
        public string Id
        {
            get;
            set;
        }

        [XmlAttribute]
        public string Name
        {
            get;
            set;
        }
    }
}

#pragma warning restore 1591