# Setup

1. Create Feature Names class somewhere in the project. It can be called anything. Example:
```csharp
public static class FeatureNames
{
    public const string FeatureA = "FeatureA";
    public const string FeatureB = "FeatureB";
    public const string FeatureC = "FeatureC";
}
```
2. Create ScriptableObject `FeatureStates` anywhere in "Resources" of your chosing.
3. Wire previously created class to "Feature Names Source".
4. Obtain an `IFeatureStateRetriever` at runtime. Manual wiring (no DI framework required):
```csharp
IFeatureStateRepository repository = new ResourceFeatureStateRepositoryRetriever("FeatureStates").Get();
IFeatureStateRetriever featureStateRetriever = new FeatureStateRetriever(repository);
```
Example DI With VContainer:
```csharp
builder.RegisterInstance(new ResourceFeatureStateRepositoryRetriever("FeatureStates").Get());
builder.Register<FeatureStateRetriever>(Lifetime.Singleton).As<IFeatureStateRetriever>();
```
> The string `"FeatureStates"` is the name of the ScriptableObject asset inside a `Resources/` folder (without extension).

# Usage

Inject `IFeatureStateRetriever` and call `IsFeatureEnabled` with one of your feature name constants:
```csharp
public class MyClass
{
    private readonly IFeatureStateRetriever _featureStateRetriever;

    public MyClass(IFeatureStateRetriever featureStateRetriever)
    {
        _featureStateRetriever = featureStateRetriever;
    }

    public void DoSomething()
    {
        if (!_featureStateRetriever.IsFeatureEnabled(FeatureNames.FeatureA))
            return;
        // feature-specific logic
    }
}
```

Always use your `FeatureNames` constants rather than inline strings to prevent typos and keep renaming safe.

# Environments

The system supports three environments, resolved automatically at runtime:

| Environment    | When active |
|----------------|-------------|
| **Editor**     | Running inside the Unity Editor |
| **Debug**      | Development builds (`Debug.isDebugBuild == true`) |
| **Production** | Release builds |

Configure which features are enabled per environment in the Inspector on your `FeatureStates` ScriptableObject.
