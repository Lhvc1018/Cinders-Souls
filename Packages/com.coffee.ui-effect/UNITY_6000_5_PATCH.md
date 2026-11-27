UIEffect 5.9.7 is embedded locally because Unity 6000.5.6f1 treats
`Object.GetInstanceID()` as obsolete with a compilation error.

The local compatibility changes replace object identity dictionary keys
with `EntityId` and use `GetEntityId().GetHashCode()` where an integer hash
is required. The original package license is preserved in LICENSE.md.

When updating UIEffect, check these changes against the new upstream release
and compile the Unity project before replacing this embedded copy.
