# Docstrings and Comments Guidelines

Clearly documented APIs and code can help our customers not lose momentum by having to go through docs or contact Customer Success. With that in mind, here are a couple of guidelines and examples of what is a good comment and what isn't.

## Guidelines
- The complexity of the docstring should match the complexity of the commented type or member. 
  - Simple data structures should have simple docstrings.
  - Complex systems should explain at least: the primary use case for the system and all assumptions the system makes with respect to its correct usage.
  - Docstring complexity should scale with the potential impact for using the method and how hard it is to debug it if its assumptions aren't met.

- Only public-facing members **need** to have docstrings --- though it's nice for our own sanity to write them for all members.
  - Document specific reason for existence and intended usage, not general description.
  - Write Docstrings for `interfaces` methods and properties as opposed to concrete implementations whenever possible (IDEs will "inherit docstrings" automagically).

- Use `<see cref="TypeName"/>`, `<paramref name="paramName"/>` and `<typeparamref name="TypeParamName"/>` to make docstrings interactable and refactoring easier for us.

- Write docstrings as you would explain things to a good Junior Engineer. 
  - Assuming a higher skill-level is incompatible with a lot of our potential customers (Unity Hobbiest Devs).
  - Assuming a lower skill-level makes writing this stuff too costly for not enough of a payoff.

- For people who want to up their comment game to new heights (because sometimes you just need a better way to explain things 😂):
  - [ASCII Table Generator](https://ozh.github.io/ascii-tables/)
  - [ASCII Tree Generator](https://ascii-tree-generator.com/)

Another subject related to this is Deprecation messages. In these, it is important for us to:
  - Leave a clear pathway for fixing/changing it:
    - If it's the new API is compatible with the old one, simply leave the new API in the message. 
    - If it's not, try and leave a message with information about how to adapt old calls to new ones.
    - If the adaptation process is long and/or complex, leave a link to a docs page explaning it.
  - Give context of the change --- As the game-maker, it's always nice to know **what** was gained by this deprecation. Otherwise, it feels like the tool is adding work for you to do without any explanation. 


Here are some examples of Good/Bad Docstrings:

#### Bad Docstrings

A bad type/interface docstring just re-iterates what the type's name tells us. It gives us no information on why the type exists, what its used for or how we can use it (and still expect it to work). Also, the TypeParam here is a language trick that enforces the interface can only ever be implemented on Attributes and that we expect you to always know the concrete implementation when calling functions related to it --- the comment could at least acknowledge that in some way.

```cs
/// <summary>
/// Interface for Attributes that enforce a unique name.
/// </summary>
/// <typeparam name="T">The type of the <see cref="Attribute"/> implementing this interface.</typeparam>
public interface IUniqueNamingAttribute<T> : IReflectionCachingAttribute<T> where T : Attribute, IUniqueNamingAttribute<T>
{
    // ...
}
```

A bad method docstring fail to explain what the function is for and its assumptions. It tells us nothing about the relevant assumption we make inside the function or what the results can be used for.
```cs
/// <summary>
/// Gets and validates attributes that must enforce a unique name.
/// </summary>
/// <param name="memberAttributePairs">The list of MemberAttributePairs.</param>
/// <returns>The validation results.</returns>
public static UniqueNameValidationResults<T> GetAndValidateUniqueNamingAttributes<T>(this IReadOnlyList<MemberAttributePair> memberAttributePairs) 
    where T : Attribute, IUniqueNamingAttribute<T>
    {
        // CODE THAT EXPECTS ALL memberAttributePairs TO HAVE ONLY IUniqueNamingAttributes
    }
```

A bad deprecation message gives you context as to why the change happened and no direction as to how you could proceed to swap it for the new recommended API.

```cs

// This is just an example ---> Don't go looking for it in our code-base.

[Obsolete("Changed to IInventoryAPI.GetUpdatedItems(string userId, string[] itemIds).")]
public Promise<InventoryView> GetItems<T>() where T : Attribute, IUniqueNamingAttribute<T>
{
    // ...
}
```


#### Good Docstrings

A good interface docstring that explains what you gain by implementing this interface and what class/system cares about it. In this case it's the `ReflectionCache` system. This gives the user of this code a path to go look into when trying to understand/debug something using this interface.

```cs
/// <summary>
/// Implement this interface over any <see cref="Attribute"/> to be able to use the existing <see cref="ReflectionCache"/> utilities 
/// to validate things with respect to Unique Names.
/// </summary>
/// <typeparam name="T">
/// The type of the <see cref="Attribute"/> implementing this interface. 
/// Just a compile-time trick to enforce this interface can only be implemented on attributes.
/// </typeparam>
public interface IUniqueNamingAttribute<T> : IReflectionCachingAttribute<T> where T : Attribute, IUniqueNamingAttribute<T>
{
    // ...
}
```

A good method docstring explains what the function is for and its assumptions. In this case, that the function expects you to pass in all `MemberAttributePairs` that you wish to check for collision at once and that they must all implement the `IUniqueNamingAttribute<T>` interface. Since most people aren't accustomed to having error data structures, it also explains what the results can be used for.
```cs
/// <summary>
/// Gets and validates attributes that must enforce a unique name.
/// Expects <paramref name="memberAttributePairs"/> to contain the entire selection of attributes whose names can't collide.
/// </summary>
/// <param name="memberAttributePairs">
/// All <see cref="MemberAttributePair"/> should contain attributes implementing <see cref="IUniqueNamingAttribute{T}"/> 
/// whose declared names cannot collide. 
/// </param>
/// <typeparam name="T">
/// Any type implementing <see cref="IUniqueNamingAttribute{T}"/> that you can use to display errors and warnings or parse valid pairs.
/// </typeparam>
/// <returns>A <see cref="UniqueNameValidationResults{T}"/> data structure with the validation results.</returns>
public static UniqueNameValidationResults<T> GetAndValidateUniqueNamingAttributes<T>(this IReadOnlyList<MemberAttributePair> memberAttributePairs) 
    where T : Attribute, IUniqueNamingAttribute<T>
    {
        // ...
    }
```

A good deprecation message lets you know why the change happened and what you gain for making the change yourself. It also leaves a clear pathway for replacing the exising version.

```cs

// This is just an example ---> Don't go looking for it in our code-base.

[Obsolete($"Was replaced by {nameof(IInventoryAPI.GetUpdatedItems)} so that it didn't depend on internal state at the back-end communication layer."
+$"You can get the logged user's userId from the ${nameof(API)} and the list of item ids from the {nameof(IInventoryApi.GetInventory)} call.")]
public Promise<InventoryView> GetItems<T>() where T : Attribute, IUniqueNamingAttribute<T>
{
    // ...
}
```





