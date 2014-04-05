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
using System.Xml;
using System.Xml.Serialization;
using NGM.CasClient.Client.Validation.Schema.Saml11.Assertion.Condition;
using NGM.CasClient.Client.Validation.Schema.Saml11.Assertion.Statement;
using NGM.CasClient.Client.Validation.Schema.Saml11.Assertion.SubjectStatement;
using NGM.CasClient.Client.Validation.Schema.XmlDsig;

#pragma warning disable 1591

namespace NGM.CasClient.Client.Validation.Schema.Saml11.Assertion
{
    [Serializable]
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [XmlType(Namespace="urn:oasis:names:tc:SAML:1.0:assertion")]
    [XmlRoot("Assertion", Namespace="urn:oasis:names:tc:SAML:1.0:assertion", IsNullable=false)]
    public class AssertionType {
        [XmlElement]
        public ConditionsType Conditions
        {
            get;
            set;
        }

        [XmlArray, XmlArrayItem("", typeof(XmlElement), IsNullable=false), XmlArrayItem("Assertion", typeof(AssertionType), IsNullable=false), XmlArrayItem("AssertionIDReference", typeof(string), DataType="NCName", IsNullable=false)]
        public object[] Advice
        {
            get;
            set;
        }

        [XmlElement("AttributeStatement", typeof(AttributeStatementType)), XmlElement("AuthenticationStatement", typeof(AuthenticationStatementType)), XmlElement("AuthorizationDecisionStatement", typeof(AuthorizationDecisionStatementType)), XmlElement("Statement", typeof(StatementAbstractType)), XmlElement("SubjectStatement", typeof(SubjectStatementAbstractType))]
        public StatementAbstractType[] Items
        {
            get;
            set;
        }

        [XmlElement(Namespace="http://www.w3.org/2000/09/xmldsig#")]
        public SignatureType Signature
        {
            get;
            set;
        }

        [XmlAttribute(DataType="integer")]
        public string MajorVersion
        {
            get;
            set;
        }

        [XmlAttribute(DataType="integer")]
        public string MinorVersion
        {
            get;
            set;
        }

        [XmlAttribute("AssertionID", DataType="ID")]
        public string AssertionId
        {
            get;
            set;
        }

        [XmlAttribute]
        public string Issuer
        {
            get;
            set;
        }

        [XmlAttribute]
        public DateTime IssueInstant
        {
            get;
            set;
        }
    }
}

#pragma warning restore 1591