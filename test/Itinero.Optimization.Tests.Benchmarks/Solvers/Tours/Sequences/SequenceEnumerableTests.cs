﻿/*
 *  Licensed to SharpSoftware under one or more contributor
 *  license agreements. See the NOTICE file distributed with this work for 
 *  additional information regarding copyright ownership.
 * 
 *  SharpSoftware licenses this file to you under the Apache License, 
 *  Version 2.0 (the "License"); you may not use this file except in 
 *  compliance with the License. You may obtain a copy of the License at
 * 
 *       http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using BenchmarkDotNet.Attributes;
using Itinero.Optimization.Solvers.Tours;
using Itinero.Optimization.Solvers.Tours.Sequences;

namespace Itinero.Optimization.Tests.Benchmarks.Solvers.Tours.Sequences
{
    public class SequenceEnumerableTests
    {
        [Benchmark]
        public int EnumerateSequence()
        {
            var total = 0;
            var tour = new Tour(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19}, 0);
            var sequences = new SequenceEnumerable(tour, 7, true);
            foreach (var s in sequences)
            {
                total += s[0];
            }
            
            return total;
        }
    }
}