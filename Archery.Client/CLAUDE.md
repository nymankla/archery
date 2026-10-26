# Archery.Client

- **No ASP.NET dependency.** Never reference `aspire.ServiceDefaults`: its `Microsoft.AspNetCore.App`
  framework reference can't load under MAUI.
- `IsTrimmable` + `IsAotCompatible`. All JSON goes through `ArcheryJsonContext`: every new wire type needs a
  `[JsonSerializable]` entry and every call passes a `JsonTypeInfo`. Reflection JSON builds fine and then
  fails at runtime in a trimmed Release APK.
- Enums are serialized as integers.
