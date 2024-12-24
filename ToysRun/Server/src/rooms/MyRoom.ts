import { Room, Client } from "colyseus";
import { EntityState } from "./schema/EntityState";
import { MyRoomState } from "./schema/MyRoomState";

export class MyRoom extends Room<MyRoomState> {

  onCreate(options: any) {
    this.setState(new MyRoomState());

    this.onMessage("MoveAvatar", (client: Client, message: any) => {
      const player_entity = this.state.entities.get(client.id);
      if (player_entity) {
        player_entity.xPos = message.transx;
        player_entity.yPos = message.transy;
        player_entity.zPos = message.transz;

        player_entity.xRot = message.rotx;
        player_entity.yRot = message.roty;
        player_entity.zRot = message.rotz;
        player_entity.wRot = message.rotw;
      }
    });

    this.onMessage("ChangeAnimation", (client: Client, message: any) => {
      const player_entity = this.state.entities.get(client.id);
      if (player_entity) {
        player_entity.animationState = message;
      }
    });
  }

  onJoin(client: Client, options: any) {
    console.log(client.sessionId, "joined as", options.type);
    const position = options.position;
    const rotation = options.rotation;
    const scale = options.scale;
    const player = new EntityState().assign({
      id: client.id,
      type: options.type,
    });

    if (position) {
      player.assign({
        xPos: position.x,
        yPos: position.y,
        zPos: position.z,
      });
    }

    if (rotation) {
      player.assign({
        xRot: rotation.x,
        yRot: rotation.y,
        zRot: rotation.z,
        wRot: rotation.w,
      });
    }

    if (scale) {
      player.assign({
        xScale: scale.x,
        yScale: scale.y,
        zScale: scale.z,
      });
    }

    this.state.entities.set(player.id, player);
  }

  onLeave(client: Client, consented: boolean) {
    this.state.entities.delete(client.id);
    console.log(client.sessionId, "left!");
  }

  onDispose() {
    console.log("room", this.roomId, "disposing...");
  }
}
