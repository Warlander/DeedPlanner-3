# Setup

1. Create a `Feature` enum somewhere in the project. Assign explicit integer values starting at 1; `0` is reserved as an unset sentinel, which makes it safe to add or remove values later. Example:
```csharp
public enum Feature
{
    FeatureA = 1,
    FeatureB = 2,
    FeatureC = 3
}
```

2. Create a concrete ScriptableObject subclass that extends `FeatureStateRepository<Feature>`. Include a `[CreateAssetMenu]` attribute so Unity can create the asset from the menu. Example:
```csharp
[CreateAssetMenu(fileName = "FeatureStates", menuName = "MyProject/Feature States")]
public class MyFeatureStateRepository : FeatureStateRepository<Feature> { }
```

3. Create a `FeatureStates` asset anywhere inside a `Resources/` folder using the menu item you declared above. Open the asset in the Inspector — the custom inspector automatically syncs all enum values as rows. Configure the Production, Debug, and Editor toggles for each feature.

4. Obtain an `IFeatureStateRetriever<Feature>` at runtime. Manual wiring (no DI framework required):
```csharp
IFeatureStateRepository<Feature> repository = new ResourceFeatureStateRepositoryRetriever<Feature>("FeatureStates").Get();
IFeatureStateRetriever<Feature> featureStateRetriever = new FeatureStateRetriever<Feature>(repository);
```
Example DI with VContainer:
```csharp
builder.RegisterInstance(new ResourceFeatureStateRepositoryRetriever<Feature>("FeatureStates").Get());
builder.Register<FeatureStateRetriever<Feature>>(Lifetime.Singleton).As<IFeatureStateRetriever<Feature>>();
```
> The string `"FeatureStates"` is the name of the ScriptableObject asset inside a `Resources/` folder (without extension).

# Usage

Inject `IFeatureStateRetriever<Feature>` and call `IsFeatureEnabled` with an enum value:
```csharp
public class MyClass
{
    private readonly IFeatureStateRetriever<Feature> _featureStateRetriever;

    public MyClass(IFeatureStateRetriever<Feature> featureStateRetriever)
    {
        _featureStateRetriever = featureStateRetriever;
    }

    public void DoSomething()
    {
        if (!_featureStateRetriever.IsFeatureEnabled(Feature.FeatureA))
            return;
        // feature-specific logic
    }
}
```

# Environments

The system supports three environments, resolved automatically at runtime:

| Environment    | When active |
|----------------|-------------|
| **Editor**     | Running inside the Unity Editor |
| **Debug**      | Development builds (`Debug.isDebugBuild == true`) |
| **Production** | Release builds |

Configure which features are enabled per environment in the Inspector on your `FeatureStates` ScriptableObject.

# Adding or Removing Features

Add or remove values from your `Feature` enum. The next time you open the `FeatureStates` asset in the Inspector, the custom editor automatically adds rows for new values and removes rows for deleted values, preserving the existing toggle settings for unchanged values.
