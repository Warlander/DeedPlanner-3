# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-03-26

### Changed
- Feature identifiers changed from strings to a user-defined enum type.
  All API methods and interfaces are now generic (`IFeatureStateRetriever<TFeature>`,
  `IFeatureStateRepository<TFeature>`, `FeatureStateRetriever<TFeature>`).
- `FeatureStateRepository` is now an abstract generic base class; clients must provide
  a concrete non-generic subclass with `[CreateAssetMenu]`.
- `FeatureState` struct renamed to `FeatureStateEntry<TFeature>`.
- `_featureNamesSource` MonoScript field removed from the ScriptableObject inspector;
  feature names are now auto-derived from the enum type.

### Removed
- String-based `IFeatureStateRetriever.IsFeatureEnabled(string)` API.
- String-based `IFeatureStateRepository` interface methods.
- Non-generic `FeatureStateRepository`, `FeatureStateRetriever`,
  `ResourceFeatureStateRepositoryRetriever` classes.

## [1.0.0] - 2026-03-22

### This is the first release of *\<Feature States\>*.
