/*
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
 
using System;
using System.Collections.Generic;
using System.Text;
using Itinero.Optimization.Algorithms.CheapestInsertion;
using Itinero.Optimization.Algorithms.Random;
using Itinero.Optimization.Algorithms.Solvers;
using Itinero.Optimization.Tours;

namespace Itinero.Optimization.VRP.NoDepot.Capacitated.Solvers.Operators
{
    /// <summary>
    /// A relocate exhange interimprovement operator.
    /// </summary>
    public class RelocateExchangeInterImprovementOperator : IOperator<float, NoDepotCVRProblem, NoDepotCVRPObjective, NoDepotCVRPSolution, float>
    {
        /// <summary>
        /// Gets the name of this operator.
        /// </summary>
        public string Name => "CROSS_REL";

        /// <summary>
        /// Returns true if this operator supports the given objective.
        /// </summary>
        /// <param name="objective"></param>
        /// <returns></returns>
        public bool Supports(NoDepotCVRPObjective objective)
        {
            return true;
        }

        /// <summary>
        /// Applies this operator.
        /// </summary>
        public bool Apply(NoDepotCVRProblem problem, NoDepotCVRPObjective objective, NoDepotCVRPSolution solution, out float delta)
        {     
            var max = problem.Max;
            int maxWindowSize = 10;

            // choose two random routes.
            var random = RandomGeneratorExtensions.GetRandom();
            var tourIdx1 = random.Generate(solution.Count);
            var tourIdx2 = random.Generate(solution.Count - 1);
            if (tourIdx2 >= tourIdx1)
            {
                tourIdx2++;
            }
            var tour1 = solution.Tour(tourIdx1);
            var tour2 = solution.Tour(tourIdx2);

            var tour1Weight = objective.Calculate(problem, solution, tourIdx1);
            var tour2Weight = objective.Calculate(problem, solution, tourIdx2);
            var totalBefore =  tour1Weight + tour2Weight;

/*             int route1_customers = tour1.Count;
            int route2_customers = tour2.Count;
 */
            var route1Cumul = objective.CalculateCumul(problem, solution, tourIdx1);
            var route2Cumul = objective.CalculateCumul(problem, solution, tourIdx2);

            // build all edge weights.
            var route1Edges = new List<Pair>(tour1.Pairs());
            var route2Edges = new List<Pair>(tour2.Pairs());
            var route1Weights = new float[route1Edges.Count];
            for (int idx = 0; idx < route1Edges.Count; idx++)
            {
                var edge = route1Edges[idx];
                route1Weights[idx] = problem.Weights[edge.From][edge.To];
            }
            var route2Weights = new float[route2Edges.Count];
            for (int idx = 0; idx < route2Edges.Count; idx++)
            {
                var edge = route2Edges[idx];
                route2Weights[idx] = problem.Weights[edge.From][edge.To];
            }

            var route2Pairs = new List<EdgePair>();
            for (int iIdx = 0; iIdx < route2Edges.Count - 2; iIdx++)
            {
                var i = route2Edges[iIdx];
                var iWeight = route2Weights[iIdx];
                var weightBeforeI = route2Cumul[iIdx];

                var kIdxMax = route2Edges.Count;
                if (kIdxMax > iIdx + 2 + maxWindowSize)
                {
                    kIdxMax = iIdx + 2 + maxWindowSize;
                }
                for (int kIdx = iIdx + 2; kIdx < kIdxMax; kIdx++)
                {
                    var k = route2Edges[kIdx];
                    var kWeight = route2Weights[kIdx];
                    var weightAfterK = route2Cumul[route2Cumul.Length - 1] - route2Cumul[kIdx + 1];
                    var weightBetweenRoute = route2Cumul[kIdx] - route2Cumul[iIdx + 1];

                    route2Pairs.Add(new EdgePair()
                    {
                        First = i,
                        FirstWeight = iWeight,
                        Second = k,
                        SecondWeight = kWeight,
                        Between = new List<int>(tour2.Between(i.To, k.From)),
                        WeightTotal = iWeight + kWeight,
                        WeightAfter = weightAfterK,
                        WeightBefore = weightBeforeI,
                        WeightBetween = weightBetweenRoute,
                        CustomersBetween = kIdx - iIdx
                    });
                }
            }

            // try to relocate all and find best pair.
            var route1Weight = route1Cumul[route1Cumul.Length - 1];
            EdgePair best = null;
            Pair? bestLocation = null;
            float bestWeight = float.MaxValue;
            foreach (var pair2 in route2Pairs)
            {
                // calculate cheapest insertion.
                Pair location;
                var weight = tour1.CalculateCheapest(problem.Weights, pair2.First.To, pair2.Second.From, out location);
                var extraRoute2 = problem.Weights[pair2.First.From][pair2.Second.To];

                // check if the result has a net-decrease.
                if (weight + extraRoute2 < pair2.WeightTotal - 0.01)
                { // there is a net decrease.
                    // calculate the real increase.
                    var newWeight = route1Weight + weight + pair2.WeightBetween;

                    // check the max.
                    if (newWeight < max && newWeight < bestWeight)
                    { // the route is smaller than max.
                        bestWeight = newWeight;
                        bestLocation = location;
                        best = pair2;
                    }
                }
            }

            if (best != null)
            {
                tour2.ReplaceEdgeFrom(best.First.From, best.Second.To);

                int previous = bestLocation.Value.From;
                foreach (int visit in best.Between)
                {
                    tour1.ReplaceEdgeFrom(previous, visit);

                    previous = visit;
                }
                tour1.ReplaceEdgeFrom(previous, bestLocation.Value.To);

                tour1Weight = objective.Calculate(problem, solution, tourIdx1);
                tour2Weight = objective.Calculate(problem, solution, tourIdx2);
                var totalAfter =  tour1Weight + tour2Weight;
                
                delta = totalBefore - totalAfter;
                return true;
            }

            delta = 0;
            return false;
        }

        private class EdgePair
        {
            public Pair First { get; set; }

            public float FirstWeight { get; set; }

            public Pair Second { get; set; }

            public float SecondWeight { get; set; }

            public List<int> Between { get; set; }

            public float WeightTotal { get; set; }

            public float WeightBefore { get; set; }

            public float WeightAfter { get; set; }

            public float WeightBetween { get; set; }

            public int CustomersBetween { get; set; }
        }
    }
}