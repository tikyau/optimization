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

using Itinero.Logistics.Objective;
using System;

namespace Itinero.Logistics.Solvers
{
    /// <summary>
    /// A wrapper for an operator, replacing the objective with another objective on each call.
    /// </summary>
    public class OperatorAndObjective<TWeight, TProblem, TObjective, TSolution, TFitness> : IOperator<TWeight, TProblem, TObjective, TSolution, TFitness>
        where TObjective : ObjectiveBase<TFitness>
        where TWeight : struct
    {
        private readonly IOperator<TWeight, TProblem, TObjective, TSolution, TFitness> _operator;
        private readonly TObjective _objective;

        /// <summary>
        /// Creates a new operator and objective wrapper.
        /// </summary>
        /// <param name="oper"></param>
        /// <param name="objective"></param>
        public OperatorAndObjective(IOperator<TWeight, TProblem, TObjective, TSolution, TFitness> oper, TObjective objective)
        {
            _operator = oper;
            _objective = objective;
        }

        /// <summary>
        /// Returns the name of the operator.
        /// </summary>
        public string Name
        {
            get { return _operator.Name; }
        }

        /// <summary>
        /// Returns true if the given object is supported.
        /// </summary>
        /// <returns></returns>
        public bool Supports(TObjective objective)
        {
            return true;
        }

        /// <summary>
        /// Returns true if there was an improvement, false otherwise.
        /// </summary>
        /// <param name="problem">The problem.</param>
        /// <param name="objective">The objective.</param>
        /// <param name="solution">The solution.</param>
        /// <param name="delta">The difference in fitness, when > 0 there was an improvement and a reduction in fitness.</param>
        /// <returns></returns>
        public bool Apply(TProblem problem, TObjective objective, TSolution solution, out TFitness delta)
        {
            return _operator.Apply(problem, _objective, solution, out delta);
        }
    }
}
