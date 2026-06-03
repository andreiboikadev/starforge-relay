namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Decides correct vs wrong insert by **color match in code** (GDD §12; guardrails §6 critical note,
    /// §16). Pure C#: no <c>MonoBehaviour</c>, no Unity statics, no side effects — it returns an
    /// <see cref="InsertOutcome"/> only. This is the pure half of the project's key XR decision: the port
    /// socket (T09) accepts *any* shard so a mismatch is still selectable, and this rule is what makes the
    /// wrong-insert path fire. Applying the outcome (heat/combo/score, eject/push-back, beams) is owned
    /// elsewhere (RoundController T06, the port-socket adapter T09, the views).
    /// </summary>
    public sealed class PortValidationService
    {
        /// <summary>
        /// Resolve a release. <paramref name="portColor"/> is the color of the port the shard was released
        /// clearly inside, or <c>null</c> when it was dropped outside any port.
        /// </summary>
        public InsertOutcome Validate(ShardColor shardColor, ShardColor? portColor)
        {
            if (portColor is null)
            {
                return InsertOutcome.NoPenalty;
            }

            return shardColor == portColor.Value ? InsertOutcome.Correct : InsertOutcome.Wrong;
        }
    }
}
