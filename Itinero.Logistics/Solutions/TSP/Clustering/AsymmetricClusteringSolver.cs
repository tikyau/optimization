﻿// Itinero.Logistics - Route optimization for .NET
// Copyright (C) 2015 Abelshausen Ben
// 
// This file is part of Itinero.
// 
// Itinero is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// Itinero is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Itinero. If not, see <http://www.gnu.org/licenses/>.

using Itinero.Logistics.Routes;
using Itinero.Logistics.Solvers;
using System;
using System.Collections.Generic;

namespace Itinero.Logistics.Solutions.TSP.Clustering
{
    /// <summary>
    /// A solver that uses the clustering algorithm to pre-process the weights and post-processes the route afterwards.
    /// </summary>
    public class AsymmetricClusteringSolver<T> : SolverBase<T, ITSP<T>, ITSPObjective<T>, IRoute>
        where T : struct
    {
        private readonly SolverBase<T, ITSP<T>, ITSPObjective<T>, IRoute> _solver;
        private readonly Func<ITSP<T>, AsymmetricClustering<T>> _createClustering;
        private readonly IOperator<T, ITSP<T>, ITSPObjective<T>, IRoute> _localSearch;

        /// <summary>
        /// Creates a new solver.
        /// </summary>
        public AsymmetricClusteringSolver(SolverBase<T, ITSP<T>, ITSPObjective<T>, IRoute> solver)
            : this(solver, (p) => new AsymmetricClustering<T>(p.WeightHandler, p.Weights))
        {

        }

        /// <summary>
        /// Creates a new solver.
        /// </summary>
        public AsymmetricClusteringSolver(SolverBase<T, ITSP<T>, ITSPObjective<T>, IRoute> solver, 
            Func<ITSP<T>, AsymmetricClustering<T>> createClustering)
            : this(solver, new LocalSearch.Local1Shift<T>(), createClustering)
        {

        }

        /// <summary>
        /// Creates a new solver.
        /// </summary>
        public AsymmetricClusteringSolver(SolverBase<T, ITSP<T>, ITSPObjective<T>, IRoute> solver, 
            IOperator<T, ITSP<T>, ITSPObjective<T>, IRoute> localSearch, Func<ITSP<T>, AsymmetricClustering<T>> createClustering)
        {
            _solver = solver;
            _createClustering = createClustering;
            _localSearch = localSearch;
        }

        /// <summary>
        /// Returns the name of this solver.
        /// </summary>
        public override string Name
        {
            get { return string.Format("CLUST_[{0} {1}]", _solver.Name, _localSearch.Name); }
        }

        /// <summary>
        /// Solves the given problem.
        /// </summary>
        /// <returns></returns>
        public override IRoute Solve(ITSP<T> problem, ITSPObjective<T> objective, out float fitness)
        {
            // do the clustering.
            var clustering =_createClustering(problem);
            clustering.Run();

            // define the new problem.
            TSPProblem<T> clusteredProblem;
            if (problem.Last.HasValue)
            { // convert the problem, also convert last->clustered last and first->clustered first.
                var clusteredLast = -1;
                var clusteredFirst = -1;
                for (var i = 0; i < clustering.Clusters.Count; i++)
                {
                    var cluster = clustering.Clusters[i];
                    if (cluster.Contains(problem.Last.Value))
                    { // ok, it's this cluster that contains the old last, so make this the new last.
                        clusteredLast = i;
                    }
                    if (cluster.Contains(problem.First))
                    { // ok, it's this cluster that contains the old first, so make this the new first.
                        clusteredFirst = i;
                    }
                }
                clusteredProblem = new TSPProblem<T>(problem.WeightHandler, clusteredFirst, clusteredLast, clustering.Weights);
            }
            else
            { // convert the problem, also convert first->clustered first.
                var clusteredFirst = -1;
                for (var i = 0; i < clustering.Clusters.Count; i++)
                {
                    var cluster = clustering.Clusters[i];
                    if (cluster.Contains(problem.First))
                    { // ok, it's this cluster that contains the old first, so make this the new first.
                        clusteredFirst = i;
                    }
                }
                clusteredProblem = new TSPProblem<T>(problem.WeightHandler, clusteredFirst, clustering.Weights);
            }

            // execute the solver.
            var clusteredRoute = _solver.Solve(clusteredProblem, objective);

            // 'uncluster' the route.
            var unclusteredRouteList = new List<int>(problem.Weights.Length);
            foreach(var clusteredCustomer in clusteredRoute)
            {
                unclusteredRouteList.AddRange(clustering.Clusters[clusteredCustomer]);
            }
            IRoute unclusteredRoute;
            if(problem.Last.HasValue && problem.First == problem.Last)
            { // build a circular route.
                unclusteredRoute = new Logistics.Routes.Route(unclusteredRouteList, problem.Last);
            }
            else if(problem.Last.HasValue)
            { // build a route with a fixed last.
                unclusteredRouteList.Remove(problem.Last.Value);
                unclusteredRouteList.Add(problem.Last.Value);
                unclusteredRoute = new Logistics.Routes.Route(unclusteredRouteList, problem.Last);
            }
            else
            { // a circular route.
                unclusteredRoute = new Logistics.Routes.Route(unclusteredRouteList);
            }

            // execute a local search to optimize the clusters.
            float delta;
            while (_localSearch.Apply(problem, objective, unclusteredRoute, out delta)) { }

            // calculate fitness.
            fitness = objective.Calculate(problem, unclusteredRoute);
            return unclusteredRoute;
        }
    }
}