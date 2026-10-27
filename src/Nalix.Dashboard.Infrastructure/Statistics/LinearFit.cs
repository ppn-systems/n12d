// Copyright (c) 2026 PPN Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

namespace Nalix.Dashboard.Infrastructure.Statistics;

/// <summary>
/// The result of an ordinary least-squares fit <c>y = Intercept + Slope * x</c>.
/// </summary>
/// <param name="Slope">The fitted slope.</param>
/// <param name="Intercept">The fitted intercept.</param>
/// <param name="RSquared">The coefficient of determination, in [0, 1].</param>
/// <param name="PValue">The two-sided p-value of H0: slope = 0.</param>
/// <param name="Count">The number of points used in the fit.</param>
public readonly record struct LinearFit(double Slope, double Intercept, double RSquared, double PValue, int Count);
