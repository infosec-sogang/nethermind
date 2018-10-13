/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using Nethermind.Blockchain.Filters.Topics;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;

namespace Nethermind.Blockchain.Filters
{
    public class Filter : FilterBase
    {
        private readonly TopicsFilter _topicsFilter;
        private readonly Address _address;
        public FilterBlock FromBlock { get; set; }
        public FilterBlock ToBlock { get; set; }
        public FilterAddress Address { get; set; }
        public IEnumerable<FilterTopic> Topics { get; set; }
        
        public Filter(FilterBlock fromBlock, FilterBlock toBlock, Address address, IEnumerable<FilterTopic> topicsFilter)
        {
            FromBlock = fromBlock;
            ToBlock = toBlock;
            Topics = topicsFilter;
            _address = address;
        }

        public bool Accepts(LogEntry logEntry)
        {
            if (Address.Address != null && Address.Address != logEntry.LoggersAddress)
            {
                return false;
            }
            
            if (Address.Addresses != null && Address.Addresses.All(a => a != logEntry.LoggersAddress))
            {
                return false;
            }

            return _topicsFilter.Accepts(logEntry);
        }
    }
}