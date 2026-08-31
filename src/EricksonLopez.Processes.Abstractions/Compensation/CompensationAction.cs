// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents an executable compensation instruction that semantically undoes or mitigates a completed step.
/// </summary>
public sealed record CompensationAction
{
    /// <summary>
    /// Gets the unique logical name of the step to compensate.
    /// </summary>
    public string StepName { get; init; }

    /// <summary>
    /// Gets the payload containing parameters required to execute the compensation.
    /// </summary>
    public object Payload { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensationAction"/> record with the specified step name and payload.
    /// </summary>
    /// <param name="stepName">The unique logical name of the step to compensate.</param>
    /// <param name="payload">The payload containing parameters required to execute the compensation.</param>
    /// <exception cref="ArgumentException"><paramref name="stepName"/> is <see langword="null"/> or white-space</exception>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/></exception>
    public CompensationAction(string stepName, object payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        StepName = stepName;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    /// <summary>
    /// Creates a new <see cref="CompensationAction"/> instance with the specified step name and payload.
    /// </summary>
    /// <typeparam name="TPayload">The type of the payload.</typeparam>
    /// <param name="stepName">The unique logical name of the step to compensate.</param>
    /// <param name="payload">The compensation action payload.</param>
    /// <returns>A new <see cref="CompensationAction"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="stepName"/> is <see langword="null"/> or white-space</exception>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/></exception>
    public static CompensationAction Create<TPayload>(string stepName, TPayload payload) where TPayload : notnull =>
        new(stepName, payload);

    /// <summary>
    /// Extracts the compensation payload cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <returns>The payload cast to <typeparamref name="T"/>.</returns>
    public T ExtractPayload<T>() => (T)Payload;

    /// <summary>
    /// Attempts to extract the compensation payload as the specified type.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <param name="payload">When this method returns, contains the typed payload if successful; otherwise, the default value for <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if the payload is of type <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
    public bool TryExtractPayload<T>(out T? payload)
    {
        if (Payload is T typed)
        {
            payload = typed;
            return true;
        }

        payload = default;
        return false;
    }
}




