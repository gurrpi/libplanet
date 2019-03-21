using System.Collections.Immutable;

namespace Libplanet.Action
{
    /// <summary>
    /// An in-game action.  Every action should be replayable, because
    /// multiple nodes in a network should execute an action and get the same
    /// result.
    /// <para>A &#x201c;class&#x201d; which implements this interface is
    /// analogous to a function, and its instance is analogous to a
    /// <a href="https://en.wikipedia.org/wiki/Partial_application">partial
    /// function application</a>, in other words, a function with some bound
    /// arguments.  Those parameters that will be bound at runtime should be
    /// represented as fields or properties in an action class, and bound
    /// argument values to these parameters should be received through
    /// a constructor parameters of that class.</para>
    /// <para>By convention, action classes are named with verb phrases, e.g.,
    /// <c>Heal</c>, <c>Sell</c>.</para>
    /// </summary>
    /// <example>
    /// The following example shows how to implement an action of a character
    /// healing someone:
    /// <code><![CDATA[
    /// using System;
    /// using System.Collections.Generic;
    /// using Libplanet.Action;
    ///
    /// // Declare the unique identifier of an action type by marking
    /// // it with ActionTypeAttribute.
    /// [ActionType("heal")]
    /// public class Heal : IAction
    /// {
    ///     const int RequiredMana = 10;
    ///     const int HealedHealthMin = 10;
    ///     const int HealedHealthMAx = 15;
    ///
    ///     // Declare properties (or fields) to store "bound" argument values.
    ///     public CharacterId HealerId { get; private set; }
    ///     public Address TargetAddress { get; private set; }
    ///     public CharacterId TargetId { get; private set; }
    ///
    ///     // Take argument values to "bind" through constructor parameters.
    ///     public Heal(CharacterId healerId,
    ///                 TargetAddress tagetAddress,
    ///                 CharacterId targetId)
    ///     {
    ///         HealerId = healerId;
    ///         TargetAddress = targetAddress;
    ///         TargetId = targetId;
    ///     }
    ///
    ///     // The main game logic belongs to here.  It takes the
    ///     // previous states through its parameter named context,
    ///     // and is offered "bound" argument values through
    ///     // its own properties (or fields).
    ///     public AddressStateMap Execute(IActionContext context)
    ///     {
    ///         // Gets the states immediately before this action is executed.
    ///         // It is merely a null delta.
    ///         IAccountStateDelta states = context.PreviousStates;
    ///
    ///         // Suppose we already declared below in-game objects "Player"
    ///         // and "Character".  These are immutable (as we highly
    ///         // recommend) and methods like ".WithFoo(bar)" mean it copies
    ///         // a game object and set new object's "Foo" property with "bar".
    ///         Player signer = states.GetState(context.Signer) as Player;
    ///         if (signer == null)
    ///             throw new Exception("Signer's player was not made.");
    ///
    ///         Character healerPrev = signer.Characters[HealerId];
    ///         if (healerPrev.Mana < RequiredMana)
    ///             throw new Exception("The healer doesn't have enough mana.");
    ///
    ///         Player receiver = states.GetState(TargetAddress) as Player;
    ///         if (receiver == null)
    ///             throw new Exception("Receiver's player was not made.");
    ///
    ///         // The amount of health to be healed is randomly determined
    ///         // in a range from 10 to 15 (see HealedHealthMin and
    ///         // HealedHealthMax).  Note that it does not use System.Random.
    ///         int healedHealth = context.Random.Next(
    ///             HealedHealthMin,
    ///             HealedHealthMax + 1  // As this is exclusive bound, add 1.
    ///         );
    ///
    ///         // Prepares new game objects that represent states after
    ///         // this action executes.  Note that Character objects are
    ///         // immutable, so their properties do not have setter but
    ///         // WithPropName() methods instead.
    ///         Character targetPrev = receiver.Characters[TargetId];
    ///         Character healerNext =
    ///             healerPrev.WithMana(healerPrev.Mana - RequiredMana);
    ///         Character targetNext = targetPrev.WithHealth(
    ///             Math.Min(
    ///                 targetPrev.Health + healedHealth,
    ///                 targetPrev.MaxHealth
    ///             )
    ///         );
    ///
    ///         // Builds a delta (dirty) from previous to next states, and
    ///         // returns it.
    ///         return states.SetState(
    ///             context.Signer,
    ///             signer.WithCharacters(HealerId, healerNext)
    ///         ).SetState(
    ///             TargetAddress,
    ///             receiver.WithCharacters(TargetId, targetNext)
    ///         );
    ///     }
    ///
    ///     // Serializes its "bound arguments" so that they are transmitted
    ///     // over network or stored to the persistent storage.
    ///     // It uses .NET's built-in serialization mechanism.
    ///     public IImmutableDictionary<string, object> PlainValue =>
    ///         new Dictionary<string, object>
    ///         {
    ///             {"healer_id", HealerId.ToInt()},
    ///             {"target_address", TargetAddress.ToByteArray()},
    ///             {"target_id", TargetId.ToInt()},
    ///         }
    ///
    ///     // Deserializes "bound arguments".  That is, it is inverse
    ///     // of PlainValue property.
    ///     public void LoadPlainValue(
    ///         IImmutableDictionary<string, object> plainValue)
    ///     {
    ///         HealerId = CharacterId.FromInt(plainValue["healer_id"] as int);
    ///         TargetAddress = new Address(
    ///             plainValue["target_address'] as byte[]
    ///         );
    ///         TargetId = CharacterId.FromInt(plainValue["target_id"] as int);
    ///     }
    /// }
    /// ]]></code></example>
    /// <remarks>Every action class should be marked with the
    /// <see cref="ActionTypeAttribute"/>.  Even if a superclass is marked
    /// with the <see cref="ActionTypeAttribute"/> its subclass also should be
    /// marked with the <see cref="ActionTypeAttribute"/>.</remarks>
    public interface IAction
    {
        /// <summary>
        /// Serializes values bound to an action, which is held by properties
        /// (or fields) of an action, so that they can be transmitted over
        /// network or saved to persistent storage.
        /// <para>Serialized values are deserialized by <see
        /// cref="LoadPlainValue(IImmutableDictionary{string,object})"/> method
        /// later.</para>
        /// <para>It uses <a href=
        /// "https://docs.microsoft.com/en-us/dotnet/standard/serialization/"
        /// >.NET's built-in serialization mechanism</a>.</para>
        /// </summary>
        /// <returns>A value which encodes this action's bound values (held
        /// by properties or fields).  It has to be <a href=
        /// "https://docs.microsoft.com/en-us/dotnet/standard/serialization/"
        /// >serializable</a>.</returns>
        /// <seealso
        /// cref="LoadPlainValue(IImmutableDictionary{string, object})"/>
        IImmutableDictionary<string, object> PlainValue { get; }

        /// <summary>
        /// Deserializes serialized data (i.e., data <see cref="PlainValue"/>
        /// property made), and then fills this action's properties (or fields)
        /// with the deserialized values.
        /// </summary>
        /// <param name="plainValue">Data (made by <see cref="PlainValue"/>
        /// property) to be deserialized and assigned to this action's
        /// properties (or fields).</param>
        /// <seealso cref="PlainValue"/>
        void LoadPlainValue(IImmutableDictionary<string, object> plainValue);

        /// <summary>
        /// Executes the main game logic of an action.  This should be
        /// <em>deterministic</em>.
        /// <para>Through the <paramref name="context"/> object,
        /// it receives information such as a transaction signer,
        /// its states immediately before the execution,
        /// and a deterministic random seed.</para>
        /// <para>Other &#x201c;bound&#x201d; information resides in the action
        /// object in itself, as its properties (or fields).</para>
        /// <para>A returned <see cref="AddressStateMap"/> object functions as
        /// a delta which shifts from previous states to next states.</para>
        /// </summary>
        /// <param name="context">A context object containing addresses that
        /// signed the transaction, states immediately before the execution,
        /// and a PRNG object which produces deterministic random numbers.
        /// See <see cref="IActionContext"/> for details.</param>
        /// <returns>A map of changed states (so-called "dirty").</returns>
        /// <remarks>This method should be deterministic:
        /// for structurally (member-wise) equal actions and <see
        /// cref="IActionContext"/>s, the same result should be returned.
        /// <para>For randomness, <em>never</em> use <see cref="System.Random"/>
        /// nor any other PRNGs provided by other than Libplanet.
        /// Use <see cref="IActionContext.Random"/> instead.</para>
        /// <para>Also do not perform I/O operations such as file system access
        /// or networking.  These bring an action indeterministic.</para>
        /// </remarks>
        /// <seealso cref="IActionContext"/>
        IAccountStateDelta Execute(IActionContext context);
    }
}