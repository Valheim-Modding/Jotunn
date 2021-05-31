# Contributing

## Setting up development environment

Please see our [guides](https://valheim-modding.github.io/Jotunn/guides/overview.html) for setting up your dev environment.

## Commiting, branching, and versioning

- For commiting, we adhere to [Convetional Commits](https://www.conventionalcommits.org/en/v1.0.0/#summary).
- For branching, we use [Git Flow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow).
- For versioning, we will be using [Semantic Versioning](https://semver.org/)

## Naming conventions, code style, and formatting

For this project, we use the same naming conventions as Microsoft's .NET framework. A [summary can be found here](https://github.com/ktaranov/naming-convention/blob/master/C%23%20Coding%20Standards%20and%20Naming%20Conventions.md).  

_Note: **ALL** public methods/properties/variables defined in public classes require XML documentation comments._  
  
Additionally, we enforce the following naming rules:
- Visibility of classes, methods, properties, and variables should always be specified, and should never be left blank (even if meant to be internal)
- Message methods (ex. `Awake`, `Update`) inherited from Unity's `Monobehaviour` should be internal or private
- Manager type class names must be suffixed with `Manager`
- Config type class names must be suffixed with `Config`
- Util type classes consisting of mainly static methods should be suffixed with either `Util` or `Helper`, and be placed in the `Utils` namespace
- All extension methods for classes should be in the `Extensions` namespace, and the class names should be suffixed with `Extension`

This is not strict, however, methods and methods defined in classes should ideally be defined in the following order:
- (public, internal, private) const variables
- (public, internal, private) static properties
- (public, internal, private) readonly variables
- (public, internal, private) variables
- (public, internal, static) methods
- (public, internal, static) methods