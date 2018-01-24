using UnityEngine;

/* Tried making this using GraphicsModule, but that means it would be
 * a PhysicalObject, requiring an AbstractPhysicalObject, which has
 * all sorts of world interaction built into it. But our ghost should
 * be ethereal!
 */

namespace ReplayGhostMod {
    public class ReplayGhost {
        public Vector2 Pos;
        public Vector2 Rot;
    }
}

/* Some interesting state to perhaps also record

public bool Malnourished {
    get {
        return false; //base.abstractCreature.world.game.session.characterStats.malnourished
    }
}

public RedsIllness redsIllness {
    get { return null; }
}

public bool isStorySession {
    get { return true; } //player.abstractCreature.world.game.IsStorySession
}

public AbstractCreature abstractCreature {
    get { return null; }
}

public Player.AnimationIndex animation {
    get;
    set;
}

public int animationFrame { get; set; }

public Player.BodyModeIndex bodyMode { get; set; }

public int flipDirection { get; set; }

public float aerobicLevel { get; set; }

public float sleepCurlUp { get; set; }

public PlayerState playerState { get; set; }

public bool Consious { get; set; }

public bool dead { get; set; }

public bool Sleeping { get; set; }

public int superLaunchJump { get; set; }

public float swimCycle { get; set; }

public float Adrenaline { get; set; }

public bool standing { get; set; }

public bool eatExternalFoodSource { get; set; }

public Vector2? handOnExternalFoodSource { get; set; }

public BodyChunk mainBodyChunk { get; set; }

public int eatExternalFoodSourceCounter { get; set; }

public global::Player.InputPackage[] input { get; private set; }

*/
