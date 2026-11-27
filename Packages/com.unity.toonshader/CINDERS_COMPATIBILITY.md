# Local compatibility patch — Unity 6000.5 / URP 17.5

This embedded copy of Toon 0.11.0-preview retains the original asset GUIDs.
Do not replace it with the registry version without migrating these changes.

`Runtime/UniversalRP/Shaders/UniversalToonInput.hlsl` supplies the Lit input contract.
URP 17.5 includes LitInput.hlsl from LitForwardPass.hlsl, creating duplicate material declarations.
The custom input therefore defines UNIVERSAL_LIT_INPUT_INCLUDED and explicitly includes SurfaceType.hlsl.
Its unused `_Surface` material field becomes `_UTSReservedSurface`, preserving buffer size without conflicting with URP.

Validation: AgentScripts/VerifyToonCompatibility.cs, VerifyToonCompatibility.Run.
Both Toon and Toon(Tessellation) pass D3D ForwardLit fragment compilation for the reported keywords.
Other keyword combinations and platforms require separate validation.
