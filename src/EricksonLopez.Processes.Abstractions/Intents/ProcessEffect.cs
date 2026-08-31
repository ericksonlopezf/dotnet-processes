// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Processes.Abstractions;

/// <summary>
/// Represents an outbound side-effect intent produced by a process state transition.
/// </summary>
public abstract record ProcessEffect
{
    /// <summary>
    /// Represents an intent to issue an asynchronous command.
    /// </summary>
    /// <param name="CommandPayload">The command payload object.</param>
    /// <param name="CommandType">Optional logical type name of the command.</param>
    public sealed record Command(object CommandPayload, string? CommandType = null) : ProcessEffect
    {
        /// <summary>
        /// Gets the command payload object.
        /// </summary>
        public object CommandPayload { get; init; } = CommandPayload ?? throw new ArgumentNullException(nameof(CommandPayload));

        /// <summary>
        /// Gets the command payload cast to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected command type.</typeparam>
        /// <returns>The cast command instance.</returns>
        public T GetPayload<T>() => (T)CommandPayload;

        /// <summary>
        /// Attempts to get the command payload as the specified type.
        /// </summary>
        /// <typeparam name="T">The expected command type.</typeparam>
        /// <param name="payload">When this method returns, contains the command payload if of type <typeparamref name="T"/>; otherwise, the default value for <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if the payload is of type <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
        public bool TryGetPayload<T>(out T? payload)
        {
            if (CommandPayload is T typed)
            {
                payload = typed;
                return true;
            }

            payload = default;
            return false;
        }
    }

    /// <summary>
    /// Represents an intent to publish an outbound domain or integration event.
    /// </summary>
    /// <param name="EventPayload">The event payload object.</param>
    /// <param name="EventType">Optional logical type name of the event.</param>
    public sealed record Event(object EventPayload, string? EventType = null) : ProcessEffect
    {
        /// <summary>
        /// Gets the event payload object.
        /// </summary>
        public object EventPayload { get; init; } = EventPayload ?? throw new ArgumentNullException(nameof(EventPayload));

        /// <summary>
        /// Gets the event payload cast to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected event type.</typeparam>
        /// <returns>The cast event instance.</returns>
        public T GetPayload<T>() => (T)EventPayload;

        /// <summary>
        /// Attempts to get the event payload as the specified type.
        /// </summary>
        /// <typeparam name="T">The expected event type.</typeparam>
        /// <param name="payload">When this method returns, contains the event payload if of type <typeparamref name="T"/>; otherwise, the default value for <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if the payload is of type <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
        public bool TryGetPayload<T>(out T? payload)
        {
            if (EventPayload is T typed)
            {
                payload = typed;
                return true;
            }

            payload = default;
            return false;
        }
    }

    /// <summary>
    /// Represents an intent to schedule a timeout or wake-up trigger after a duration.
    /// </summary>
    /// <param name="Delay">The duration to wait before triggering the timeout.</param>
    /// <param name="TimeoutTrigger">The trigger payload to deliver when the timeout expires.</param>
    /// <param name="TriggerType">Optional logical type of the trigger.</param>
    public sealed record ScheduleTimeout(TimeSpan Delay, object TimeoutTrigger, string? TriggerType = null) : ProcessEffect
    {
        /// <summary>
        /// Gets the timeout trigger payload object.
        /// </summary>
        public object TimeoutTrigger { get; init; } = TimeoutTrigger ?? throw new ArgumentNullException(nameof(TimeoutTrigger));

        /// <summary>
        /// Gets the timeout trigger payload cast to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected trigger type.</typeparam>
        /// <returns>The cast trigger instance.</returns>
        public T GetTrigger<T>() => (T)TimeoutTrigger;

        /// <summary>
        /// Attempts to get the timeout trigger payload as the specified type.
        /// </summary>
        /// <typeparam name="T">The expected trigger type.</typeparam>
        /// <param name="trigger">When this method returns, contains the trigger payload if of type <typeparamref name="T"/>; otherwise, the default value for <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if the trigger is of type <typeparamref name="T"/>; otherwise, <see langword="false"/>.</returns>
        public bool TryGetTrigger<T>(out T? trigger)
        {
            if (TimeoutTrigger is T typed)
            {
                trigger = typed;
                return true;
            }

            trigger = default;
            return false;
        }
    }

    /// <summary>
    /// Represents an intent to execute a compensating action.
    /// </summary>
    /// <param name="Action">The compensation action details.</param>
    public sealed record Compensation(CompensationAction Action) : ProcessEffect
    {
        /// <summary>
        /// Gets the compensation action details.
        /// </summary>
        public CompensationAction Action { get; init; } = Action ?? throw new ArgumentNullException(nameof(Action));
    }

    /// <summary>
    /// Creates a new <see cref="Command"/> effect with the specified payload and command type.
    /// </summary>
    /// <typeparam name="T">The command payload type.</typeparam>
    /// <param name="payload">The command payload instance.</param>
    /// <param name="commandType">The optional logical command type name.</param>
    /// <returns>A new <see cref="Command"/> effect instance.</returns>
    public static Command CreateCommand<T>(T payload, string? commandType = null) where T : notnull =>
        new(payload, commandType ?? typeof(T).Name);

    /// <summary>
    /// Creates a new <see cref="Event"/> effect with the specified payload and event type.
    /// </summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="payload">The event payload instance.</param>
    /// <param name="eventType">The optional logical event type name.</param>
    /// <returns>A new <see cref="Event"/> effect instance.</returns>
    public static Event CreateEvent<T>(T payload, string? eventType = null) where T : notnull =>
        new(payload, eventType ?? typeof(T).Name);

    /// <summary>
    /// Creates a new <see cref="ScheduleTimeout"/> effect with the specified delay, trigger, and trigger type.
    /// </summary>
    /// <typeparam name="T">The trigger payload type.</typeparam>
    /// <param name="delay">The duration before the timeout expires.</param>
    /// <param name="trigger">The trigger payload to deliver when the timeout expires.</param>
    /// <param name="triggerType">The optional logical trigger type name.</param>
    /// <returns>A new <see cref="ScheduleTimeout"/> effect instance.</returns>
    public static ScheduleTimeout CreateTimeout<T>(TimeSpan delay, T trigger, string? triggerType = null) where T : notnull =>
        new(delay, trigger, triggerType ?? typeof(T).Name);

    /// <summary>
    /// Creates a new <see cref="Compensation"/> effect with the specified action.
    /// </summary>
    /// <param name="action">The compensation action details.</param>
    /// <returns>A new <see cref="Compensation"/> effect instance.</returns>
    public static Compensation CreateCompensation(CompensationAction action) =>
        new(action);

    /// <summary>
    /// Creates a new <see cref="Compensation"/> effect with the specified step name and payload.
    /// </summary>
    /// <typeparam name="T">The compensation payload type.</typeparam>
    /// <param name="stepName">The unique logical name of the step to compensate.</param>
    /// <param name="payload">The compensation action payload.</param>
    /// <returns>A new <see cref="Compensation"/> effect instance.</returns>
    public static Compensation CreateCompensation<T>(string stepName, T payload) where T : notnull =>
        new(CompensationAction.Create(stepName, payload));
}




