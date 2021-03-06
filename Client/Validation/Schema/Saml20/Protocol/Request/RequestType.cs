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
using NGM.CasClient.Client.Validation.Schema.Saml20.Assertion;

#pragma warning disable 1591

namespace NGM.CasClient.Client.Validation.Schema.Saml20.Protocol.Request
{
    [Serializable]
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [XmlType(Namespace="urn:oasis:names:tc:SAML:2.0:profiles:SSO:ecp")]
    [XmlRoot("Request", Namespace="urn:oasis:names:tc:SAML:2.0:profiles:SSO:ecp", IsNullable=false)]
    public class RequestType {
        [XmlElement(Namespace="urn:oasis:names:tc:SAML:2.0:assertion")]
        public NameIdType Issuer
        {
            get;
            set;
        }

        [XmlElement("IDPList", Namespace = "urn:oasis:names:tc:SAML:2.0:protocol")]
        public IdpListType IdpList
        {
            get;
            set;
        }

        [XmlAttribute("mustUnderstand", Form=System.Xml.Schema.XmlSchemaForm.Qualified, Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
        public bool MustUnderstand
        {
            get;
            set;
        }

        [XmlAttribute("actor", Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://schemas.xmlsoap.org/soap/envelope/", DataType = "anyURI")]
        public string Actor
        {
            get;
            set;
        }

        [XmlAttribute]
        public string ProviderName
        {
            get;
            set;
        }

        [XmlAttribute]
        public bool IsPassive
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool IsPassiveSpecified
        {
            get;
            set;
        }
    }
}

#pragma warning restore 1591