// ==============================================================================
// 
// EveHQ - An Eve-Online™ character assistance application
// Copyright © 2005-2015  EveHQ Development Team
//   
// This file is part of EveHQ.
//  
// The source code for EveHQ is free and you may redistribute 
// it and/or modify it under the terms of the MIT License. 
// 
// Refer to the NOTICES file in the root folder of EVEHQ source
// project for details of 3rd party components that are covered
// under their own, separate licenses.
// 
// EveHQ is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the MIT 
// license below for details.
// 
// ------------------------------------------------------------------------------
// 
// The MIT License (MIT)
// 
// Copyright © 2005-2015  EveHQ Development Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------
// 
// <copyright file="MarketOrder.cs" company="EveHQ Development Team">
//     Copyright © 2005-2015  EveHQ Development Team
// </copyright>
// 
// ==============================================================================

namespace EveHQ.Market
{
    /// <summary>
    ///     Market object used to describe an single order (buy or sell) for an item
    /// </summary>
    using System;

    public class MarketOrder
    {
        #region Public Properties

        /// <summary>Gets or sets the duration.</summary>
        public int Duration { get; set; }

        /// <summary>Gets or sets the expires.</summary>
        public DateTimeOffset Expires { get; set; }

        /// <summary>Gets or sets the freshness.</summary>
        public DateTimeOffset Freshness { get; set; }

        /// <summary>Gets or sets a value indicating whether is buy order.</summary>
        public bool IsBuyOrder { get; set; }

        /// <summary>Gets or sets the issued.</summary>
        public DateTimeOffset Issued { get; set; }

        /// <summary>Gets or sets the item id.</summary>
        public int ItemId { get; set; }

        /// <summary>Gets or sets the jumps.</summary>
        public int Jumps { get; set; }

        /// <summary>Gets or sets the min quantity.</summary>
        public int MinQuantity { get; set; }

        /// <summary>Gets or sets the order id.</summary>
        public long OrderId { get; set; }

        /// <summary>Gets or sets the order range.</summary>
        public string OrderRange { get; set; }

        /// <summary>Gets or sets the price.</summary>
        public double Price { get; set; }

        /// <summary>Gets or sets the quantity entered.</summary>
        public int QuantityEntered { get; set; }

        /// <summary>Gets or sets the quantity remaining.</summary>
        public int QuantityRemaining { get; set; }

        /// <summary>Gets or sets the region id.</summary>
        public int RegionId { get; set; }

        /// <summary>Gets or sets the security.</summary>
        public double Security { get; set; }

        /// <summary>Gets or sets the solar system id.</summary>
        public int SolarSystemId { get; set; }

        /// <summary>Gets or sets the station id.</summary>
        public long LocationId { get; set; }

        /// <summary>Gets or sets the station name.</summary>
        public string StationName { get; set; }

        #endregion
    }
}