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

namespace NGM.CasClient.Client
{
    /// <summary>
    /// Lists the possible states of the gateway feature. 
    /// </summary>
    /// <author>Scott Holodak</author>
    public enum GatewayStatus
    {
        /// <summary>
        /// Gateway authentication has not been attempted or the client is not 
        /// accepting session cookies
        /// </summary>
        NotAttempted,

        /// <summary>
        /// Gateway authentication is in progress
        /// </summary>
        Attempting,
            
        /// <summary>
        /// The Gateway authentication attempt was successful.  Gateway 
        /// authentication will not be attempted in subsequent requests
        /// </summary>
        Success,

        /// <summary>
        /// The Gateway authentication attempt was attempted, but a service 
        /// ticket was not returned.  Gateway authentication will not be 
        /// attempted in subsequent requests.
        /// </summary>
        Failed
    }
}