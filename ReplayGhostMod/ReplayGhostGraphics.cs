using UnityEngine;

/* 
 * Todo: richer recording of slugcat, but that becomes much like building
 * a networking protocol
 */


namespace ReplayGhostMod {
    public class ReplayGhostGraphics : CosmeticSprite {
        private readonly ReplayGhost _ghost;

        public ReplayGhostGraphics(ReplayGhost ghost) {
            _ghost = ghost;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("KrakenMask1", true);
            sLeaser.sprites[0].color = new Color(1f, 0f, 0f, 1f);

            AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            Vector2 vector = _ghost.Pos - camPos; //Vector2.Lerp(ghost.bodyChunks[0].lastPos, ghost.bodyChunks[0].pos, timeStacker)
            sLeaser.sprites[0].x = vector.x;
            sLeaser.sprites[0].y = vector.y;
            sLeaser.sprites[0].isVisible = true;
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
        }
        
    }
}