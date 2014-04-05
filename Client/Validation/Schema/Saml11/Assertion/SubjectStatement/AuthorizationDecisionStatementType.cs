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

#pragma warning disable 1591

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace NGM.CasClient.Client.Validation.Schema.Saml11.Assertion.SubjectStatement
{
    [Serializable]
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [XmlType(Namespace="urn:oasis:names:tc:SAML:1.0:assertion")]
    [XmlRoot("AuthorizationDecisionStatement", Namespace="urn:oasis:names:tc:SAML:1.0:assertion", IsNullable=false)]
    public class AuthorizationDecisionStatementType : SubjectStatementAbstractType {
        [XmlElement("Action")]
        public ActionType[] Action
        {
            get;
            set;
        }

        [XmlArray, XmlArrayItem("Assertion", typeof(AssertionType), IsNullable=false), XmlArrayItem("AssertionIDReference", typeof(string), DataType="NCName", IsNullable=false)]
        public object[] Evidence
        {
            get;
            set;
        }

        [XmlAttribute(DataType="anyURI")]
        public string Resource
        {
            get;
            set;
        }

        [XmlAttribute]
        public DecisionType Decision
        {
            get;
            set;
        }
    }
}

#pragma warning restore 1591