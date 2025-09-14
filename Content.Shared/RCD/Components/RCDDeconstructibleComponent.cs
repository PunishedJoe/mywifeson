using Content.Shared.Random;
using Content.Shared.RCD.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.RCD.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RCDSystem))]
public sealed partial class RCDDeconstructableComponent : Component
{
    /// <summary>
    /// Number of charges consumed when the deconstruction is completed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Cost = 1;

    /// <summary>
    /// The length of the deconstruction-
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Delay = 1f;

    /// <summary>
    /// The visual effect that plays during deconstruction
    /// </summary>
    [DataField("fx"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? Effect = null;

    /// <summary>
    /// Toggles whether this entity is deconstructable or not
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Deconstructable = true;


    /// <summary>
    /// Toggles whether this entity is deconstructable by the RPD or not
    /// </summary>
    [DataField("rpd"), ViewVariables(VVAccess.ReadWrite)]
    public bool RpdDeconstructable = false;

    /// <summary>
    /// Toggles whether this entity is scrappable by the RCD or not
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RcdScrappable = false;

    /// <summary>
    /// What this entity will drop when scrapped with the RCD?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<WeightedRandomEntityPrototype>>? ScrapResults = null;
}
